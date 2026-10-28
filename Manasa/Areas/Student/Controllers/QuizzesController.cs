using Manasa.Data;
using Manasa.Localization;
using Manasa.Models;
using Manasa.Services;
using Manasa.ViewModels.Student;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Localization;

namespace Manasa.Areas.Student.Controllers;

[Area("Student")]
[Authorize(Roles = "Student")]
public sealed class QuizzesController(ApplicationDbContext db, QuizService quizService, IStringLocalizer<SharedResource> text) : Controller
{
    public async Task<IActionResult> Index()
    {
        var studentId = UserId();
        var quizzes = await db.Quizzes.AsNoTracking().Where(q => q.IsPublished && q.Course.IsPublished)
            .OrderBy(q => q.Title).Select(q => new StudentQuizRowViewModel
            {
                Id = q.Id, Title = q.Title, CourseTitle = q.Course.Title, Description = q.Description,
                QuestionCount = q.Questions.Count, MaximumAttempts = q.MaximumAttempts,
                AttemptsUsed = q.Attempts.Count(a => a.StudentId == studentId),
                HasInProgressAttempt = q.Attempts.Any(a => a.StudentId == studentId && a.Status == QuizAttemptStatus.InProgress)
            }).ToListAsync();
        return View(new StudentQuizListViewModel { Quizzes = quizzes });
    }

    public async Task<IActionResult> Details(int id)
    {
        var studentId = UserId();
        var quiz = await db.Quizzes.AsNoTracking().Where(q => q.Id == id && q.IsPublished && q.Course.IsPublished)
            .Select(q => new StudentQuizDetailsViewModel
            {
                Id = q.Id, Title = q.Title, CourseTitle = q.Course.Title, Description = q.Description,
                PassingScorePercent = q.PassingScorePercent, TimeLimitMinutes = q.TimeLimitMinutes,
                MaximumAttempts = q.MaximumAttempts, AttemptsUsed = q.Attempts.Count(a => a.StudentId == studentId),
                HasInProgressAttempt = q.Attempts.Any(a => a.StudentId == studentId && a.Status == QuizAttemptStatus.InProgress)
            }).SingleOrDefaultAsync();
        return quiz is null ? NotFound() : View(quiz);
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Start(int id)
    {
        var attemptId = await quizService.StartAttemptAsync(UserId(), id);
        if (attemptId is null)
        {
            TempData["ErrorMessage"] = text["This quiz is unavailable or your attempt limit has been reached."].Value;
            return RedirectToAction(nameof(Details), new { id });
        }
        return RedirectToAction(nameof(Take), new { id = attemptId.Value });
    }

    public async Task<IActionResult> Take(long id)
    {
        var attempt = await db.QuizAttempts.AsNoTracking().Where(a => a.Id == id && a.StudentId == UserId())
            .Select(a => new { a.Id, a.Status, a.Quiz.Title, a.TimeLimitMinutesSnapshot }).SingleOrDefaultAsync();
        if (attempt is null) return NotFound();
        if (attempt.Status != QuizAttemptStatus.InProgress) return RedirectToAction(nameof(Result), new { id });
        var questions = await quizService.GetAttemptQuestionsAsync(id, UserId());
        if (questions is null) return NotFound();
        return View(new TakeQuizViewModel
        {
            AttemptId = id, QuizTitle = attempt.Title, TimeLimitMinutes = attempt.TimeLimitMinutesSnapshot,
            Questions = questions.Select((q, index) => new TakeQuizQuestionViewModel
            {
                Id = q.QuestionId, Text = q.Text, Order = index + 1,
                Options = q.Options.Select(o => new TakeQuizOptionViewModel { Id = o.OptionId, Text = o.Text }).ToList()
            }).ToList()
        });
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Submit(SubmitQuizViewModel model)
    {
        if (!ModelState.IsValid) return RedirectToAction(nameof(Take), new { id = model.AttemptId });
        try
        {
            var result = await quizService.SubmitAsync(model.AttemptId, UserId(),
                model.Answers.Where(answer => answer.OptionId.HasValue)
                    .Select(answer => new SubmittedAnswer(answer.QuestionId, answer.OptionId!.Value)).ToList());
            if (result is null) return NotFound();
            return RedirectToAction(nameof(Result), new { id = result.AttemptId });
        }
        catch (ArgumentException)
        {
            return BadRequest();
        }
    }

    public async Task<IActionResult> Result(long id)
    {
        var attempt = await db.QuizAttempts.AsNoTracking().Include(a => a.Answers)
            .SingleOrDefaultAsync(a => a.Id == id && a.StudentId == UserId());
        if (attempt is null) return NotFound();
        if (attempt.Status == QuizAttemptStatus.InProgress) return RedirectToAction(nameof(Take), new { id });
        return View(new QuizResultViewModel
        {
            QuizId = attempt.QuizId, AttemptId = attempt.Id, QuizTitle = attempt.QuizTitleSnapshot,
            AttemptNumber = attempt.AttemptNumber, Earned = attempt.EarnedScore, Maximum = attempt.MaximumScore,
            Percentage = attempt.Percentage, Passed = attempt.Passed, Status = attempt.Status, SubmittedAtUtc = attempt.SubmittedAtUtc,
            Answers = attempt.Answers.OrderBy(a => a.QuestionId).Select(a => new QuizResultAnswerViewModel
            {
                Question = a.QuestionTextSnapshot, SelectedAnswer = a.SelectedAnswerSnapshot, IsCorrect = a.IsCorrect,
                Earned = a.EarnedPoints, Maximum = a.MaximumPoints, Explanation = a.ExplanationSnapshot
            }).ToList()
        });
    }

    public async Task<IActionResult> History(int quizId)
    {
        var quiz = await db.Quizzes.AsNoTracking().Where(q => q.Id == quizId && q.Attempts.Any(a => a.StudentId == UserId()))
            .Select(q => new { q.Id, q.Title }).SingleOrDefaultAsync();
        if (quiz is null) return NotFound();
        var attempts = await db.QuizAttempts.AsNoTracking().Where(a => a.QuizId == quizId && a.StudentId == UserId())
            .OrderByDescending(a => a.AttemptNumber).Select(a => new QuizResultViewModel
            {
                QuizId = a.QuizId, AttemptId = a.Id, QuizTitle = a.QuizTitleSnapshot, AttemptNumber = a.AttemptNumber,
                Earned = a.EarnedScore, Maximum = a.MaximumScore, Percentage = a.Percentage, Passed = a.Passed,
                Status = a.Status, SubmittedAtUtc = a.SubmittedAtUtc
            }).ToListAsync();
        return View(new StudentQuizHistoryViewModel { QuizId = quiz.Id, QuizTitle = quiz.Title, Attempts = attempts });
    }

    private string UserId() => User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value
        ?? throw new InvalidOperationException("Authenticated Student has no Identity user ID claim.");
}

public sealed class StudentQuizHistoryViewModel
{
    public int QuizId { get; init; }
    public string QuizTitle { get; init; } = string.Empty;
    public IReadOnlyList<QuizResultViewModel> Attempts { get; init; } = [];
}
