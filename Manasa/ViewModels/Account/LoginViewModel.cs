using System.ComponentModel.DataAnnotations;

namespace Manasa.ViewModels.Account;

public class LoginViewModel
{
    [Required, StringLength(256)]
    public string UserNameOrEmail { get; set; } = string.Empty;

    [Required, DataType(DataType.Password)]
    public string Password { get; set; } = string.Empty;

    public bool RememberMe { get; set; }

    public string? ReturnUrl { get; set; }
}
