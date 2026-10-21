using System.ComponentModel.DataAnnotations;

namespace Manasa.Models;

public class Course
{
    public int Id { get; set; }

    public int SubjectId { get; set; }

    public int? TeacherProfileId { get; set; }

    [Required, MaxLength(180)]
    public string Title { get; set; } = string.Empty;

    [MaxLength(4000)]
    public string? Description { get; set; }

    [MaxLength(500)]
    public string? ThumbnailUrl { get; set; }

    public bool IsPublished { get; set; }

    public DateTime CreatedAtUtc { get; set; }

    public Subject Subject { get; set; } = null!;

    public TeacherProfile? TeacherProfile { get; set; }

    public ICollection<CoursePackage> Packages { get; set; } = new List<CoursePackage>();
}
