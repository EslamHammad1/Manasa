namespace Manasa.ViewModels.Student;

public sealed class DashboardViewModel
{
    public string StudentName { get; init; } = string.Empty;

    public IReadOnlyList<PublishedCourseViewModel> Courses { get; init; } = [];
    public int TotalXp { get; init; }
    public int CurrentStreak { get; init; }
    public int LongestStreak { get; init; }
    public IReadOnlyList<StudentBadgeViewModel> Badges { get; init; } = [];
    public IReadOnlyList<StudentRecentQuizViewModel> RecentQuizzes { get; init; } = [];
    public IReadOnlyList<AchievementProgressViewModel> AchievementProgress { get; init; } = [];
}

public sealed class AchievementProgressViewModel
{
    public string NameResourceKey { get; init; } = string.Empty;
    public int Current { get; init; }
    public int Target { get; init; }
}

public sealed class StudentBadgeViewModel
{
    public string Code { get; init; } = string.Empty;
    public string NameResourceKey { get; init; } = string.Empty;
    public string DescriptionResourceKey { get; init; } = string.Empty;
    public DateTime EarnedAtUtc { get; init; }
}

public sealed class StudentRecentQuizViewModel
{
    public long AttemptId { get; init; }
    public int QuizId { get; init; }
    public string QuizTitle { get; init; } = string.Empty;
    public decimal Percentage { get; init; }
    public bool Passed { get; init; }
    public Manasa.Models.QuizAttemptStatus Status { get; init; }
    public DateTime StartedAtUtc { get; init; }
}

public sealed class PublishedCourseViewModel
{
    public int Id { get; init; }

    public string Title { get; init; } = string.Empty;

    public string Subject { get; init; } = string.Empty;

    public string? Description { get; init; }

    public string? ThumbnailUrl { get; init; }
}

public sealed class CourseDetailsViewModel
{
    public string Title { get; init; } = string.Empty;

    public string Subject { get; init; } = string.Empty;

    public string? Description { get; init; }

    public string? ThumbnailUrl { get; init; }

    public string? TeacherName { get; init; }
}
