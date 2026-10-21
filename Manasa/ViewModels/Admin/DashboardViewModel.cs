namespace Manasa.ViewModels.Admin;

public class DashboardViewModel
{
    public int StudentCount { get; set; }

    public int TeacherCount { get; set; }

    public int SubjectCount { get; set; }

    public int PublishedCourseCount { get; set; }

    public int CourseCount { get; set; }

    public int PackageCount { get; set; }

    public IReadOnlyList<RecentCourseViewModel> RecentCourses { get; set; } = [];
}

public class RecentCourseViewModel
{
    public string Title { get; set; } = string.Empty;

    public string Subject { get; set; } = string.Empty;

    public DateTime CreatedAtUtc { get; set; }

    public bool IsPublished { get; set; }
}
