using System.ComponentModel.DataAnnotations;

namespace Manasa.Models;

public class Quiz
{
    public int Id { get; set; }
    public int CourseId { get; set; }
    [Required, MaxLength(180)] public string Title { get; set; } = string.Empty;
    [MaxLength(2000)] public string? Description { get; set; }
    public bool IsPublished { get; set; }
    public decimal PassingScorePercent { get; set; } = 70m;
    public int? TimeLimitMinutes { get; set; }
    public int? MaximumAttempts { get; set; }
    public bool ShowExplanationsAfterSubmission { get; set; }
    public DateTime CreatedAtUtc { get; set; }
    public DateTime UpdatedAtUtc { get; set; }
    public Course Course { get; set; } = null!;
    public ICollection<QuizQuestion> Questions { get; set; } = new List<QuizQuestion>();
    public ICollection<QuizAttempt> Attempts { get; set; } = new List<QuizAttempt>();
}

public class QuizQuestion
{
    public int Id { get; set; }
    public int QuizId { get; set; }
    [Required, MaxLength(2000)] public string Text { get; set; } = string.Empty;
    public QuizQuestionType Type { get; set; }
    public decimal Points { get; set; } = 1m;
    public int DisplayOrder { get; set; }
    [MaxLength(2000)] public string? Explanation { get; set; }
    public Quiz Quiz { get; set; } = null!;
    public ICollection<QuizOption> Options { get; set; } = new List<QuizOption>();
}

public enum QuizQuestionType { MultipleChoice = 1, TrueFalse = 2 }

public class QuizOption
{
    public int Id { get; set; }
    public int QuizQuestionId { get; set; }
    [Required, MaxLength(500)] public string Text { get; set; } = string.Empty;
    public int DisplayOrder { get; set; }
    public bool IsCorrect { get; set; }
    public QuizQuestion QuizQuestion { get; set; } = null!;
}

public enum QuizAttemptStatus { InProgress = 1, Submitted = 2, Expired = 3 }

public class QuizAttempt
{
    public long Id { get; set; }
    [Required] public string StudentId { get; set; } = string.Empty;
    public int QuizId { get; set; }
    public int AttemptNumber { get; set; }
    [Required, MaxLength(180)] public string QuizTitleSnapshot { get; set; } = string.Empty;
    public DateTime StartedAtUtc { get; set; }
    public DateTime? SubmittedAtUtc { get; set; }
    public QuizAttemptStatus Status { get; set; }
    public decimal EarnedScore { get; set; }
    public decimal MaximumScore { get; set; }
    public decimal Percentage { get; set; }
    public bool Passed { get; set; }
    public decimal PassingScorePercentSnapshot { get; set; }
    public int? TimeLimitMinutesSnapshot { get; set; }
    public bool ShowExplanationsSnapshot { get; set; }
    [Required] public string QuestionSnapshotJson { get; set; } = "[]";
    public byte[] RowVersion { get; set; } = [];
    public ApplicationUser Student { get; set; } = null!;
    public Quiz Quiz { get; set; } = null!;
    public ICollection<QuizAttemptAnswer> Answers { get; set; } = new List<QuizAttemptAnswer>();
}

public class QuizAttemptAnswer
{
    public long Id { get; set; }
    public long QuizAttemptId { get; set; }
    public int QuestionId { get; set; }
    [Required, MaxLength(2000)] public string QuestionTextSnapshot { get; set; } = string.Empty;
    [MaxLength(500)] public string? SelectedAnswerSnapshot { get; set; }
    public bool IsCorrect { get; set; }
    public decimal EarnedPoints { get; set; }
    public decimal MaximumPoints { get; set; }
    [MaxLength(2000)] public string? ExplanationSnapshot { get; set; }
    public QuizAttempt Attempt { get; set; } = null!;
}

public class StudentXpAward
{
    public long Id { get; set; }
    [Required] public string StudentId { get; set; } = string.Empty;
    [Required, MaxLength(40)] public string EventType { get; set; } = string.Empty;
    [Required, MaxLength(100)] public string EventKey { get; set; } = string.Empty;
    public int Points { get; set; }
    public DateTime AwardedAtUtc { get; set; }
    public ApplicationUser Student { get; set; } = null!;
}

public class BadgeDefinition
{
    [MaxLength(40)] public string Code { get; set; } = string.Empty;
    [MaxLength(100)] public string NameResourceKey { get; set; } = string.Empty;
    [MaxLength(120)] public string DescriptionResourceKey { get; set; } = string.Empty;
    public bool IsActive { get; set; } = true;
}

public class StudentBadge
{
    public long Id { get; set; }
    [Required] public string StudentId { get; set; } = string.Empty;
    [Required, MaxLength(40)] public string BadgeCode { get; set; } = string.Empty;
    public DateTime EarnedAtUtc { get; set; }
    public ApplicationUser Student { get; set; } = null!;
    public BadgeDefinition Badge { get; set; } = null!;
}

public class StudentLearningDay
{
    public long Id { get; set; }
    [Required] public string StudentId { get; set; } = string.Empty;
    public DateOnly ActivityDate { get; set; }
    public DateTime LastActivityAtUtc { get; set; }
    public ApplicationUser Student { get; set; } = null!;
}
