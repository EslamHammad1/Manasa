using Manasa.Models;

namespace Manasa.ViewModels.Student;

public sealed class StudentQuizListViewModel
{
    public IReadOnlyList<StudentQuizRowViewModel> Quizzes { get; init; } = [];
}
public sealed class StudentQuizRowViewModel
{
    public int Id { get; init; }
    public string Title { get; init; } = string.Empty;
    public string CourseTitle { get; init; } = string.Empty;
    public string? Description { get; init; }
    public int QuestionCount { get; init; }
    public int? MaximumAttempts { get; init; }
    public int AttemptsUsed { get; init; }
    public bool HasInProgressAttempt { get; init; }
}
public sealed class StudentQuizDetailsViewModel
{
    public int Id { get; init; }
    public string Title { get; init; } = string.Empty;
    public string CourseTitle { get; init; } = string.Empty;
    public string? Description { get; init; }
    public decimal PassingScorePercent { get; init; }
    public int? TimeLimitMinutes { get; init; }
    public int? MaximumAttempts { get; init; }
    public int AttemptsUsed { get; init; }
    public bool HasInProgressAttempt { get; init; }
}
public sealed class TakeQuizViewModel
{
    public long AttemptId { get; init; }
    public string QuizTitle { get; init; } = string.Empty;
    public int? TimeLimitMinutes { get; init; }
    public IReadOnlyList<TakeQuizQuestionViewModel> Questions { get; init; } = [];
}
public sealed class TakeQuizQuestionViewModel
{
    public int Id { get; init; }
    public string Text { get; init; } = string.Empty;
    public int Order { get; init; }
    public IReadOnlyList<TakeQuizOptionViewModel> Options { get; init; } = [];
}
public sealed class TakeQuizOptionViewModel
{
    public int Id { get; init; }
    public string Text { get; init; } = string.Empty;
}
public sealed class SubmitQuizViewModel
{
    public long AttemptId { get; set; }
    public List<SubmitQuizAnswerViewModel> Answers { get; set; } = [];
}
public sealed class SubmitQuizAnswerViewModel
{
    public int QuestionId { get; set; }
    public int? OptionId { get; set; }
}
public sealed class QuizResultViewModel
{
    public int QuizId { get; init; }
    public long AttemptId { get; init; }
    public string QuizTitle { get; init; } = string.Empty;
    public int AttemptNumber { get; init; }
    public decimal Earned { get; init; }
    public decimal Maximum { get; init; }
    public decimal Percentage { get; init; }
    public bool Passed { get; init; }
    public QuizAttemptStatus Status { get; init; }
    public DateTime? SubmittedAtUtc { get; init; }
    public IReadOnlyList<QuizResultAnswerViewModel> Answers { get; init; } = [];
}
public sealed class QuizResultAnswerViewModel
{
    public string Question { get; init; } = string.Empty;
    public string? SelectedAnswer { get; init; }
    public bool IsCorrect { get; init; }
    public decimal Earned { get; init; }
    public decimal Maximum { get; init; }
    public string? Explanation { get; init; }
}
