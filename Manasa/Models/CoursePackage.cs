using System.ComponentModel.DataAnnotations;

namespace Manasa.Models;

public class CoursePackage
{
    public int Id { get; set; }

    public int CourseId { get; set; }

    [Required, MaxLength(120)]
    public string Name { get; set; } = string.Empty;

    [MaxLength(2000)]
    public string? Description { get; set; }

    public decimal Price { get; set; }

    public int AccessDurationDays { get; set; }

    public bool IsActive { get; set; } = true;

    public DateTime CreatedAtUtc { get; set; }

    public Course Course { get; set; } = null!;
}
