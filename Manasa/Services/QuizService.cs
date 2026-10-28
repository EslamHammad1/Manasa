using System.Data;
using System.Text.Json;
using Manasa.Data;
using Manasa.Models;
using Microsoft.EntityFrameworkCore;

namespace Manasa.Services;

public sealed record QuizSnapshot(int QuestionId, string Text, QuizQuestionType Type, decimal Points,
    string? Explanation, IReadOnlyList<OptionSnapshot> Options);
public sealed record OptionSnapshot(int OptionId, string Text, bool IsCorrect);
public sealed record QuizResult(long AttemptId, decimal Earned, decimal Maximum, decimal Percentage, bool Passed,
    QuizAttemptStatus Status, DateTime? SubmittedAtUtc);
public sealed record SubmittedAnswer(int QuestionId, int OptionId);

public sealed class QuizService(ApplicationDbContext db, TimeProvider clock)
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);
    private static readonly TimeZoneInfo ActivityTimeZone = TimeZoneInfo.Utc;
    private const int PassingQuizXp = 25;

    public async Task<long?> StartAttemptAsync(string studentId, int quizId, CancellationToken cancellationToken = default)
    {
        await using var transaction = await db.Database.BeginTransactionAsync(IsolationLevel.Serializable, cancellationToken);
        var quiz = await db.Quizzes.Include(q => q.Questions).ThenInclude(q => q.Options)
            .SingleOrDefaultAsync(q => q.Id == quizId && q.IsPublished && q.Course.IsPublished, cancellationToken);
        if (quiz is null || !IsValidPublishedQuiz(quiz)) return null;

        var attempts = await db.QuizAttempts.Where(a => a.StudentId == studentId && a.QuizId == quizId)
            .OrderByDescending(a => a.AttemptNumber).ToListAsync(cancellationToken);
        var inProgress = attempts.FirstOrDefault(a => a.Status == QuizAttemptStatus.InProgress);
        var now = clock.GetUtcNow().UtcDateTime;
        if (inProgress is not null)
        {
            if (inProgress.TimeLimitMinutesSnapshot is not int limit || now <= inProgress.StartedAtUtc.AddMinutes(limit))
                return inProgress.Id;
            inProgress.Status = QuizAttemptStatus.Expired;
            inProgress.SubmittedAtUtc = now;
            await db.SaveChangesAsync(cancellationToken);
        }
        if (quiz.MaximumAttempts is int maximumAttempts && attempts.Count >= maximumAttempts)
        {
            await transaction.CommitAsync(cancellationToken);
            return null;
        }

        var questions = quiz.Questions.OrderBy(q => q.DisplayOrder).Select(q => new QuizSnapshot(q.Id, q.Text, q.Type,
            q.Points, q.Explanation, q.Options.OrderBy(o => o.DisplayOrder)
                .Select(o => new OptionSnapshot(o.Id, o.Text, o.IsCorrect)).ToList())).ToList();
        var attempt = new QuizAttempt
        {
            StudentId = studentId, QuizId = quizId, AttemptNumber = attempts.Count + 1,
            QuizTitleSnapshot = quiz.Title,
            StartedAtUtc = now, Status = QuizAttemptStatus.InProgress,
            MaximumScore = questions.Sum(q => q.Points), PassingScorePercentSnapshot = quiz.PassingScorePercent,
            TimeLimitMinutesSnapshot = quiz.TimeLimitMinutes, ShowExplanationsSnapshot = quiz.ShowExplanationsAfterSubmission,
            QuestionSnapshotJson = JsonSerializer.Serialize(questions, JsonOptions)
        };
        db.QuizAttempts.Add(attempt);
        await db.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
        return attempt.Id;
    }

    public async Task<IReadOnlyList<QuizSnapshot>?> GetAttemptQuestionsAsync(long attemptId, string studentId,
        CancellationToken cancellationToken = default)
    {
        var attempt = await db.QuizAttempts.AsNoTracking()
            .SingleOrDefaultAsync(a => a.Id == attemptId && a.StudentId == studentId, cancellationToken);
        return attempt is null ? null : JsonSerializer.Deserialize<List<QuizSnapshot>>(attempt.QuestionSnapshotJson, JsonOptions) ?? [];
    }

    public async Task<QuizResult?> SubmitAsync(long attemptId, string studentId, IReadOnlyCollection<SubmittedAnswer> submitted,
        CancellationToken cancellationToken = default)
    {
        await using var transaction = await db.Database.BeginTransactionAsync(IsolationLevel.Serializable, cancellationToken);
        var attempt = await db.QuizAttempts.Include(a => a.Quiz).ThenInclude(q => q.Course)
            .Include(a => a.Answers).SingleOrDefaultAsync(a => a.Id == attemptId && a.StudentId == studentId, cancellationToken);
        if (attempt is null) return null;
        if (attempt.Status != QuizAttemptStatus.InProgress)
        {
            await transaction.CommitAsync(cancellationToken);
            return ToResult(attempt);
        }

        var now = clock.GetUtcNow().UtcDateTime;
        var questions = JsonSerializer.Deserialize<List<QuizSnapshot>>(attempt.QuestionSnapshotJson, JsonOptions) ?? [];
        if (attempt.TimeLimitMinutesSnapshot is int minutes && now > attempt.StartedAtUtc.AddMinutes(minutes))
        {
            attempt.Status = QuizAttemptStatus.Expired;
            attempt.SubmittedAtUtc = now;
        }
        else
        {
            if (submitted.GroupBy(a => a.QuestionId).Any(g => g.Count() > 1))
                throw new ArgumentException("Submitted answers contain an unknown question or option.", nameof(submitted));
            var grade = QuizGrader.Grade(questions, submitted.ToDictionary(answer => answer.QuestionId, answer => answer.OptionId),
                attempt.PassingScorePercentSnapshot);
            foreach (var question in questions)
            {
                var graded = grade.Answers.Single(answer => answer.QuestionId == question.QuestionId);
                var selected = graded.SelectedOptionId is int optionId
                    ? question.Options.Single(option => option.OptionId == optionId) : null;
                attempt.Answers.Add(new QuizAttemptAnswer
                {
                    QuestionId = question.QuestionId, QuestionTextSnapshot = question.Text,
                    SelectedAnswerSnapshot = selected?.Text, IsCorrect = graded.IsCorrect, EarnedPoints = graded.EarnedPoints,
                    MaximumPoints = question.Points,
                    ExplanationSnapshot = attempt.ShowExplanationsSnapshot ? question.Explanation : null
                });
            }
            attempt.EarnedScore = grade.EarnedScore;
            attempt.MaximumScore = grade.MaximumScore;
            attempt.Percentage = grade.Percentage;
            attempt.Passed = grade.Passed;
            attempt.Status = QuizAttemptStatus.Submitted;
            attempt.SubmittedAtUtc = now;

            // A pass awards XP once per quiz; later retries cannot farm points.
            if (attempt.Passed)
                await AwardXpOnceAsync(studentId, "QuizPass", attempt.QuizId.ToString(), PassingQuizXp, now, cancellationToken);
            var day = DateOnly.FromDateTime(TimeZoneInfo.ConvertTimeFromUtc(now, ActivityTimeZone));
            var activity = await db.StudentLearningDays.SingleOrDefaultAsync(d => d.StudentId == studentId && d.ActivityDate == day, cancellationToken);
            if (activity is null) db.StudentLearningDays.Add(new StudentLearningDay { StudentId = studentId, ActivityDate = day, LastActivityAtUtc = now });
            else activity.LastActivityAtUtc = now;
            await EvaluateBadgesAsync(studentId, attempt, now, cancellationToken);
        }

        try
        {
            await db.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);
        }
        catch (DbUpdateConcurrencyException)
        {
            await transaction.RollbackAsync(cancellationToken);
            db.ChangeTracker.Clear();
            var existing = await db.QuizAttempts.AsNoTracking().SingleOrDefaultAsync(a => a.Id == attemptId && a.StudentId == studentId, cancellationToken);
            return existing is null ? null : ToResult(existing);
        }
        catch (DbUpdateException)
        {
            await transaction.RollbackAsync(cancellationToken);
            db.ChangeTracker.Clear();
            var existing = await db.QuizAttempts.AsNoTracking().SingleOrDefaultAsync(a => a.Id == attemptId && a.StudentId == studentId, cancellationToken);
            if (existing?.Status != QuizAttemptStatus.InProgress) return existing is null ? null : ToResult(existing);
            throw;
        }
        return ToResult(attempt);
    }

    public async Task<(int TotalXp, int CurrentStreak, int LongestStreak)> GetGamificationSummaryAsync(string studentId,
        CancellationToken cancellationToken = default)
    {
        var xp = await db.StudentXpAwards.Where(a => a.StudentId == studentId).SumAsync(a => (int?)a.Points, cancellationToken) ?? 0;
        var dates = await db.StudentLearningDays.Where(d => d.StudentId == studentId).OrderByDescending(d => d.ActivityDate)
            .Select(d => d.ActivityDate).ToListAsync(cancellationToken);
        var set = dates.ToHashSet();
        var today = DateOnly.FromDateTime(TimeZoneInfo.ConvertTimeFromUtc(clock.GetUtcNow().UtcDateTime, ActivityTimeZone));
        var cursor = set.Contains(today) ? today : today.AddDays(-1);
        var current = 0;
        while (set.Contains(cursor)) { current++; cursor = cursor.AddDays(-1); }
        var longest = 0; var run = 0; DateOnly? previous = null;
        foreach (var date in dates.Order()) { run = previous is null || date.DayNumber == previous.Value.DayNumber + 1 ? run + 1 : 1; longest = Math.Max(longest, run); previous = date; }
        return (xp, current, longest);
    }

    private async Task AwardXpOnceAsync(string studentId, string eventType, string eventKey, int points, DateTime now, CancellationToken ct)
    {
        if (!await db.StudentXpAwards.AnyAsync(a => a.StudentId == studentId && a.EventType == eventType && a.EventKey == eventKey, ct))
            db.StudentXpAwards.Add(new StudentXpAward { StudentId = studentId, EventType = eventType, EventKey = eventKey, Points = points, AwardedAtUtc = now });
    }

    private async Task EvaluateBadgesAsync(string studentId, QuizAttempt attempt, DateTime now, CancellationToken ct)
    {
        await AwardBadgeOnceAsync(studentId, "FirstQuiz", now, ct);
        if (attempt.Percentage == 100m) await AwardBadgeOnceAsync(studentId, "PerfectScore", now, ct);
        var passedCourseIds = await db.QuizAttempts.Where(a => a.StudentId == studentId && a.Passed)
            .Select(a => a.Quiz.CourseId).Distinct().ToListAsync(ct);
        var currentCourseId = await db.Quizzes.Where(q => q.Id == attempt.QuizId).Select(q => q.CourseId).SingleAsync(ct);
        var uniquePassed = passedCourseIds.Count + (attempt.Passed && !passedCourseIds.Contains(currentCourseId) ? 1 : 0);
        if (uniquePassed >= 5) await AwardBadgeOnceAsync(studentId, "QuizMaster", now, ct);
        var totalXp = await db.StudentXpAwards.Where(a => a.StudentId == studentId).SumAsync(a => (int?)a.Points, ct) ?? 0;
        if (attempt.Passed && !await db.StudentXpAwards.AnyAsync(a => a.StudentId == studentId && a.EventType == "QuizPass" && a.EventKey == attempt.QuizId.ToString(), ct))
            totalXp += PassingQuizXp;
        if (totalXp >= 100) await AwardBadgeOnceAsync(studentId, "LearningMilestone", now, ct);
    }

    private async Task AwardBadgeOnceAsync(string studentId, string code, DateTime now, CancellationToken ct)
    {
        if (!await db.StudentBadges.AnyAsync(b => b.StudentId == studentId && b.BadgeCode == code, ct))
            db.StudentBadges.Add(new StudentBadge { StudentId = studentId, BadgeCode = code, EarnedAtUtc = now });
    }

    private static bool IsValidPublishedQuiz(Quiz quiz) => quiz.PassingScorePercent is >= 0 and <= 100 &&
        quiz.TimeLimitMinutes is null or > 0 && quiz.MaximumAttempts is null or > 0 && quiz.Questions.Count > 0 &&
        quiz.Questions.All(q => Enum.IsDefined(q.Type) && q.Points > 0 && q.Options.Count >= 2 && q.Options.Count(o => o.IsCorrect) == 1 &&
            (q.Type != QuizQuestionType.TrueFalse || q.Options.Count == 2));

    private static QuizResult ToResult(QuizAttempt attempt) => new(attempt.Id, attempt.EarnedScore, attempt.MaximumScore,
        attempt.Percentage, attempt.Passed, attempt.Status, attempt.SubmittedAtUtc);
}
