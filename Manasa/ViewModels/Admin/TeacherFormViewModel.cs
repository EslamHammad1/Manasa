using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace Manasa.ViewModels.Admin;

public class TeacherFormViewModel
{
    public int Id { get; set; }

    [Required, StringLength(160)]
    public string FullName { get; set; } = string.Empty;

    [Required, EmailAddress, StringLength(256)]
    public string Email { get; set; } = string.Empty;

    [StringLength(4000)]
    public string? Biography { get; set; }

    [Url, StringLength(500)]
    public string? ProfileImageUrl { get; set; }

    [StringLength(160)]
    public string? Specialization { get; set; }

    [Range(0, 100)]
    public int? ExperienceYears { get; set; }

    public bool IsActive { get; set; } = true;

    public int[] SelectedSubjectIds { get; set; } = [];

    public IEnumerable<SelectListItem> Subjects { get; set; } = [];

    public string? AccountSetupUrl { get; set; }
}
