using System.ComponentModel.DataAnnotations;

namespace Manasa.Models;

public class StudentLessonProgress
{
    public int Id { get; set; }

    [Required]
    public string StudentId { get; set; } = string.Empty;

    public int LessonId { get; set; }

    public bool IsCompleted { get; set; }

    public DateTime? CompletedAtUtc { get; set; }

    public DateTime? LastAccessedAtUtc { get; set; }

    public ApplicationUser Student { get; set; } = null!;

    public CourseLesson Lesson { get; set; } = null!;
}
