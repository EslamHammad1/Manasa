using System.ComponentModel.DataAnnotations;

namespace Manasa.ViewModels.Admin;

public class StudentListItemViewModel
{
    public string UserId { get; set; } = string.Empty;

    public string UserName { get; set; } = string.Empty;

    public string? DisplayName { get; set; }

    public string? Email { get; set; }

    public bool IsActive { get; set; }
}

public class StudentListViewModel
{
    public string? Search { get; set; }

    public int PageNumber { get; set; }

    public int TotalPages { get; set; }

    public IReadOnlyList<StudentListItemViewModel> Items { get; set; } = [];
}

public class StudentFormViewModel
{
    public string? UserId { get; set; }

    public string? UserName { get; set; }

    [Required, StringLength(160), Display(Name = "Full name")]
    public string DisplayName { get; set; } = string.Empty;

    [Required, EmailAddress, StringLength(256), Display(Name = "Email address")]
    public string Email { get; set; } = string.Empty;

    public string? AccountSetupUrl { get; set; }
}
