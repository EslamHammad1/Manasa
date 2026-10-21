using System.ComponentModel.DataAnnotations;

namespace Manasa.Models;

public class TeacherProfile
{
    public int Id { get; set; }

    [Required]
    public string UserId { get; set; } = string.Empty;

    [Required, MaxLength(160)]
    public string FullName { get; set; } = string.Empty;

    [MaxLength(4000)]
    public string? Biography { get; set; }

    [MaxLength(500)]
    public string? ProfileImageUrl { get; set; }

    [MaxLength(160)]
    public string? Specialization { get; set; }

    public int? ExperienceYears { get; set; }

    public bool IsActive { get; set; } = true;

    public DateTime CreatedAtUtc { get; set; }

    public ApplicationUser User { get; set; } = null!;

    public ICollection<SubjectTeacher> SubjectTeachers { get; set; } = new List<SubjectTeacher>();

    public ICollection<Course> Courses { get; set; } = new List<Course>();
}
