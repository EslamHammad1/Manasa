using System.ComponentModel.DataAnnotations;

namespace Manasa.Models;

public class CourseLesson
{
    public int Id { get; set; }

    public int CourseId { get; set; }

    [Required, MaxLength(180)]
    public string Title { get; set; } = string.Empty;

    [MaxLength(1000)]
    public string? Description { get; set; }

    [MaxLength(20000)]
    public string? TextContent { get; set; }

    [MaxLength(500)]
    public string? VideoUrl { get; set; }

    [Range(0, 100000)]
    public int DisplayOrder { get; set; }

    public bool IsPublished { get; set; }

    public DateTime CreatedAtUtc { get; set; }

    public DateTime? UpdatedAtUtc { get; set; }

    public Course Course { get; set; } = null!;

    public ICollection<StudentLessonProgress> StudentProgress { get; set; } = new List<StudentLessonProgress>();
}
