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
}

public sealed class CourseDetailsViewModel
{
    public string Title { get; init; } = string.Empty;

    public string Subject { get; init; } = string.Empty;

    public string? Description { get; init; }

    public string? ThumbnailUrl { get; init; }

    public string? TeacherName { get; init; }
}
