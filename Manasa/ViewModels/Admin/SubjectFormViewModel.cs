using System.ComponentModel.DataAnnotations;

namespace Manasa.ViewModels.Admin;

public class SubjectFormViewModel
{
    public int Id { get; set; }

    [Required, StringLength(120), Display(Name = "Subject name")]
    public string Name { get; set; } = string.Empty;

    [StringLength(2000), Display(Name = "Description")]
    public string? Description { get; set; }

    [Url, StringLength(500), Display(Name = "Image URL")]
    public string? ImageUrl { get; set; }

    public bool IsActive { get; set; } = true;

    [Range(0, int.MaxValue), Display(Name = "Display order")]
    public int DisplayOrder { get; set; }
}
