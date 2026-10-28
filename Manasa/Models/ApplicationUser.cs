using Microsoft.AspNetCore.Identity;

namespace Manasa.Models;

public class ApplicationUser : IdentityUser
{
    public string? DisplayName { get; set; }

    public DateTime CreatedAtUtc { get; set; }

    public ICollection<StudentLessonProgress> LessonProgress { get; set; } = new List<StudentLessonProgress>();
}
