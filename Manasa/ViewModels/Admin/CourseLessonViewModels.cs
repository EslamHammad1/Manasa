using System.ComponentModel.DataAnnotations;

namespace Manasa.ViewModels.Admin;

public sealed class CourseLessonListViewModel
{
    public int CourseId { get; init; }

    public string CourseTitle { get; init; } = string.Empty;

    public IReadOnlyList<CourseLessonListItemViewModel> Lessons { get; init; } = [];
}

public sealed class CourseLessonListItemViewModel
{
    public int Id { get; init; }

    public string Title { get; init; } = string.Empty;

    public int DisplayOrder { get; init; }

    public bool IsPublished { get; init; }

    public bool HasText { get; init; }

    public bool HasVideo { get; init; }

    public int ProgressRecordCount { get; init; }
}

public sealed class CourseLessonFormViewModel
{
    public int Id { get; set; }

    [Range(1, int.MaxValue)]
    public int CourseId { get; set; }

    public string CourseTitle { get; set; } = string.Empty;

    [Required, StringLength(180)]
    public string Title { get; set; } = string.Empty;

    [StringLength(1000)]
    public string? Description { get; set; }

    [StringLength(20000)]
    public string? TextContent { get; set; }

    [StringLength(500)]
    public string? VideoUrl { get; set; }

    [Range(0, 100000)]
    public int DisplayOrder { get; set; }

    public bool IsPublished { get; set; }
}
