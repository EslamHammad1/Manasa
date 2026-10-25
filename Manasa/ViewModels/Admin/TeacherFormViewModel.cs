using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace Manasa.ViewModels.Admin;

public class TeacherFormViewModel
{
    public int Id { get; set; }

    [Required, StringLength(160), Display(Name = "Full name")]
    public string FullName { get; set; } = string.Empty;

    [Required, EmailAddress, StringLength(256), Display(Name = "Email address")]
    public string Email { get; set; } = string.Empty;

    [StringLength(4000), Display(Name = "Biography")]
    public string? Biography { get; set; }

    [Url, StringLength(500), Display(Name = "Profile image URL")]
    public string? ProfileImageUrl { get; set; }

    [StringLength(160), Display(Name = "Specialization")]
    public string? Specialization { get; set; }

    [Range(0, 100), Display(Name = "Years of experience")]
    public int? ExperienceYears { get; set; }

    public bool IsActive { get; set; } = true;

    public int[] SelectedSubjectIds { get; set; } = [];

    public IEnumerable<SelectListItem> Subjects { get; set; } = [];

    public string? AccountSetupUrl { get; set; }
}
