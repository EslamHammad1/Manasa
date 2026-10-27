using System.ComponentModel.DataAnnotations;
using Manasa.Models;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace Manasa.ViewModels.Admin;

public sealed class QuizFormViewModel
{
    public int Id { get; set; }
    [Required, Range(1, int.MaxValue)] public int CourseId { get; set; }
    [Required, StringLength(180)] public string Title { get; set; } = string.Empty;
    [StringLength(2000)] public string? Description { get; set; }
    [Range(typeof(decimal), "0", "100")] public decimal PassingScorePercent { get; set; } = 70m;
    [Range(1, 1440)] public int? TimeLimitMinutes { get; set; }
    [Range(1, 1000)] public int? MaximumAttempts { get; set; }
    public bool ShowExplanationsAfterSubmission { get; set; }
    public bool IsPublished { get; set; }
    public IReadOnlyList<SelectListItem> Courses { get; set; } = [];
}

public sealed class QuizQuestionFormViewModel
{
    public int QuizId { get; set; }
    public int? Id { get; set; }
    [Required, StringLength(2000)] public string Text { get; set; } = string.Empty;
    public QuizQuestionType Type { get; set; } = QuizQuestionType.MultipleChoice;
    [Range(typeof(decimal), "0.01", "999999")] public decimal Points { get; set; } = 1m;
    [Range(1, 1000)] public int DisplayOrder { get; set; } = 1;
    [StringLength(2000)] public string? Explanation { get; set; }
    [Required] public string OptionsText { get; set; } = string.Empty;
    [Range(1, 20)] public int CorrectOptionNumber { get; set; } = 1;
}

public sealed class AdminQuizListViewModel
{
    public IReadOnlyList<AdminQuizRowViewModel> Quizzes { get; init; } = [];
}
public sealed class AdminQuizRowViewModel
{
    public int Id { get; init; }
    public string Title { get; init; } = string.Empty;
    public string CourseTitle { get; init; } = string.Empty;
    public bool IsPublished { get; init; }
    public int QuestionCount { get; init; }
    public int AttemptCount { get; init; }
}

public sealed class AdminQuizQuestionsViewModel
{
    public int QuizId { get; init; }
    public string QuizTitle { get; init; } = string.Empty;
    public bool IsPublished { get; init; }
    public IReadOnlyList<AdminQuestionRowViewModel> Questions { get; init; } = [];
}
public sealed class AdminQuestionRowViewModel
{
    public int Id { get; init; }
    public string Text { get; init; } = string.Empty;
    public string Type { get; init; } = string.Empty;
    public decimal Points { get; init; }
    public int DisplayOrder { get; init; }
}

public sealed class AdminQuizAttemptsViewModel
{
    public int QuizId { get; init; }
    public string QuizTitle { get; init; } = string.Empty;
    public IReadOnlyList<AdminQuizAttemptRowViewModel> Attempts { get; init; } = [];
}
public sealed class AdminQuizPreviewViewModel
{
    public string Title { get; init; } = string.Empty;
    public string CourseTitle { get; init; } = string.Empty;
    public string? Description { get; init; }
    public decimal PassingScorePercent { get; init; }
    public int? TimeLimitMinutes { get; init; }
    public IReadOnlyList<AdminQuestionPreviewViewModel> Questions { get; init; } = [];
}
public sealed class AdminQuestionPreviewViewModel
{
    public string Text { get; init; } = string.Empty;
    public QuizQuestionType Type { get; init; }
    public decimal Points { get; init; }
    public IReadOnlyList<AdminOptionPreviewViewModel> Options { get; init; } = [];
}
public sealed class AdminOptionPreviewViewModel
{
    public string Text { get; init; } = string.Empty;
    public bool IsCorrect { get; init; }
}
public sealed class AdminQuizAttemptRowViewModel
{
    public long Id { get; init; }
    public string StudentName { get; init; } = string.Empty;
    public int AttemptNumber { get; init; }
    public decimal Percentage { get; init; }
    public bool Passed { get; init; }
    public QuizAttemptStatus Status { get; init; }
    public DateTime StartedAtUtc { get; init; }
    public DateTime? SubmittedAtUtc { get; init; }
}
