using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace Manasa.ViewModels.Admin;

public class CourseFormViewModel
{
    public int Id { get; set; }

    [Required, Range(1, int.MaxValue)]
    public int SubjectId { get; set; }

    public int? TeacherProfileId { get; set; }

    [Required, StringLength(180)]
    public string Title { get; set; } = string.Empty;

    [StringLength(4000)]
    public string? Description { get; set; }

    [Url, StringLength(500)]
    public string? ThumbnailUrl { get; set; }

    public bool IsPublished { get; set; }

    public IEnumerable<SelectListItem> Subjects { get; set; } = [];

    public IEnumerable<SelectListItem> Teachers { get; set; } = [];
}
