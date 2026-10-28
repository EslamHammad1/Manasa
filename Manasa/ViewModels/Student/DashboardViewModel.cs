namespace Manasa.ViewModels.Student;

public sealed class DashboardViewModel
{
    public string StudentName { get; init; } = string.Empty;

    public IReadOnlyList<PublishedCourseViewModel> Courses { get; init; } = [];
}

public sealed class PublishedCourseViewModel
{
    public int Id { get; init; }

    public string Title { get; init; } = string.Empty;

    public string Subject { get; init; } = string.Empty;

    public string? Description { get; init; }

    public string? ThumbnailUrl { get; init; }

    public int CompletedLessons { get; set; }

    public int TotalLessons { get; set; }

    public int CompletionPercentage { get; set; }

    public int? ResumeLessonId { get; set; }
}

public sealed class CourseDetailsViewModel
{
    public int Id { get; init; }

    public string Title { get; init; } = string.Empty;

    public string Subject { get; init; } = string.Empty;

    public string? Description { get; init; }

    public string? ThumbnailUrl { get; init; }

    public string? TeacherName { get; init; }

    public int CompletedLessons { get; set; }

    public int TotalLessons { get; set; }

    public int CompletionPercentage { get; set; }

    public int? ResumeLessonId { get; set; }

    public IReadOnlyList<CourseLessonSummaryViewModel> Lessons { get; set; } = [];
}

public sealed class CourseLessonSummaryViewModel
{
    public int Id { get; init; }

    public string Title { get; init; } = string.Empty;

    public int DisplayOrder { get; init; }

    public bool IsCompleted { get; init; }

    public DateTime? LastAccessedAtUtc { get; init; }
}

public sealed class LessonLearningViewModel
{
    public int CourseId { get; init; }

    public string CourseTitle { get; init; } = string.Empty;

    public int LessonId { get; init; }

    public string Title { get; init; } = string.Empty;

    public string? Description { get; init; }

    public string? TextContent { get; init; }

    public string? VideoEmbedUrl { get; init; }

    public string? OriginalVideoUrl { get; init; }

    public bool IsCompleted { get; init; }

    public int LessonNumber { get; init; }

    public int CompletedLessons { get; init; }

    public int TotalLessons { get; init; }

    public int CompletionPercentage { get; init; }

    public int? PreviousLessonId { get; init; }

    public int? NextLessonId { get; init; }

    public IReadOnlyList<LessonNavigationItemViewModel> Lessons { get; init; } = [];
}

public sealed class LessonNavigationItemViewModel
{
    public int Id { get; init; }

    public string Title { get; init; } = string.Empty;

    public int DisplayOrder { get; init; }

    public bool IsCompleted { get; init; }

    public bool IsCurrent { get; init; }
}
