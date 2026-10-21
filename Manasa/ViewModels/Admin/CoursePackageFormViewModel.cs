using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace Manasa.ViewModels.Admin;

public class CoursePackageFormViewModel
{
    public int Id { get; set; }

    [Required, Range(1, int.MaxValue)]
    public int CourseId { get; set; }

    [Required, StringLength(120)]
    public string Name { get; set; } = string.Empty;

    [StringLength(2000)]
    public string? Description { get; set; }

    [Range(typeof(decimal), "0", "9999999999999999.99")]
    public decimal Price { get; set; }

    [Range(1, 36500)]
    public int AccessDurationDays { get; set; }

    public bool IsActive { get; set; } = true;

    public IEnumerable<SelectListItem> Courses { get; set; } = [];
}
