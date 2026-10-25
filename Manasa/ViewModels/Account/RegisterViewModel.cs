using System.ComponentModel.DataAnnotations;

namespace Manasa.ViewModels.Account;

public class RegisterViewModel
{
    [Required, StringLength(100)]
    [Display(Name = "Full name")]
    public string FullName { get; set; } = string.Empty;

    [Required, EmailAddress, StringLength(256)]
    [Display(Name = "Email address")]
    public string Email { get; set; } = string.Empty;

    [Phone, StringLength(32)]
    [Display(Name = "Phone number")]
    public string? PhoneNumber { get; set; }

    [Required, DataType(DataType.Password)]
    [StringLength(100, MinimumLength = 6)]
    [Display(Name = "Password")]
    public string Password { get; set; } = string.Empty;

    [Required, DataType(DataType.Password)]
    [Compare(nameof(Password), ErrorMessage = "The passwords do not match.")]
    [Display(Name = "Confirm password")]
    public string ConfirmPassword { get; set; } = string.Empty;

    [MustBeTrue(ErrorMessage = "Please accept the Terms and Conditions.")]
    [Display(Name = "I agree to the Terms and Conditions")]
    public bool AcceptTerms { get; set; }
}

public sealed class MustBeTrueAttribute : ValidationAttribute
{
    public override bool IsValid(object? value) => value is true;
}
