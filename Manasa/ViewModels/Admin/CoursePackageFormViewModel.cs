using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace Manasa.ViewModels.Admin;

public class CoursePackageFormViewModel
{
    public int Id { get; set; }

    [Required, Range(1, int.MaxValue)]
    public int CourseId { get; set; }

    [Required, StringLength(120), Display(Name = "Name")]
    public string Name { get; set; } = string.Empty;

    [StringLength(2000), Display(Name = "Description")]
    public string? Description { get; set; }

    [Range(typeof(decimal), "0", "9999999999999999.99"), Display(Name = "Price")]
    public decimal Price { get; set; }

    [Range(1, 36500), Display(Name = "Access duration (days)")]
    public int AccessDurationDays { get; set; }

    public bool IsActive { get; set; } = true;

    public IEnumerable<SelectListItem> Courses { get; set; } = [];
}
