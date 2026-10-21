using System.ComponentModel.DataAnnotations;

namespace Manasa.ViewModels.Admin;

public class SubjectFormViewModel
{
    public int Id { get; set; }

    [Required, StringLength(120)]
    public string Name { get; set; } = string.Empty;

    [StringLength(2000)]
    public string? Description { get; set; }

    [Url, StringLength(500)]
    public string? ImageUrl { get; set; }

    public bool IsActive { get; set; } = true;

    [Range(0, int.MaxValue)]
    public int DisplayOrder { get; set; }
}
