using Manasa.Models;
using Manasa.Localization;
using Manasa.ViewModels.Account;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Manasa.Data;
using Microsoft.Extensions.Localization;

namespace Manasa.Controllers;

public class AccountController(
    UserManager<ApplicationUser> userManager,
    SignInManager<ApplicationUser> signInManager,
    ApplicationDbContext dbContext,
    ILogger<AccountController> logger,
    IStringLocalizer<SharedResource> text) : Controller
{
    [AllowAnonymous]
    public IActionResult Register()
    {
        return View(new RegisterViewModel());
    }

    [HttpPost, AllowAnonymous, ValidateAntiForgeryToken]
    public async Task<IActionResult> Register(RegisterViewModel model)
    {
        if (!ModelState.IsValid)
        {
            return View(model);
        }

        if (await userManager.FindByEmailAsync(model.Email.Trim()) is not null)
        {
            ModelState.AddModelError(nameof(model.Email), text["An account with this email already exists."]);
            return View(model);
        }

        try
        {
            await using var transaction = await dbContext.Database.BeginTransactionAsync();
            var userName = await GetNextStudentUserNameAsync();
            var student = new ApplicationUser
            {
                UserName = userName,
                Email = model.Email.Trim(),
                PhoneNumber = string.IsNullOrWhiteSpace(model.PhoneNumber) ? null : model.PhoneNumber.Trim(),
                DisplayName = model.FullName.Trim(),
                LockoutEnabled = true
            };

            var createResult = await userManager.CreateAsync(student, model.Password);
            if (!createResult.Succeeded)
            {
                await transaction.RollbackAsync();
                AddRegistrationErrors(createResult);
                return View(model);
            }

            var roleResult = await userManager.AddToRoleAsync(student, "Student");
            if (!roleResult.Succeeded)
            {
                await transaction.RollbackAsync();
                logger.LogError("Could not assign Student role to registered account {UserId}: {Errors}",
                    student.Id, string.Join(", ", roleResult.Errors.Select(error => error.Code)));
                ModelState.AddModelError(string.Empty, text["We could not complete your registration. Please try again."]);
                return View(model);
            }

            await transaction.CommitAsync();
            TempData["RegisteredUserName"] = userName;
            return RedirectToAction(nameof(Login));
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "Student registration failed.");
            ModelState.AddModelError(string.Empty, text["We could not complete your registration. Please try again."]);
            return View(model);
        }
    }

    [AllowAnonymous]
    public IActionResult Login(string? returnUrl = null)
    {
        return View(new LoginViewModel { ReturnUrl = returnUrl });
    }

    [HttpPost, AllowAnonymous, ValidateAntiForgeryToken]
    public async Task<IActionResult> Login(LoginViewModel model)
    {
        if (!ModelState.IsValid)
        {
            return View(model);
        }

        var identifier = model.UserNameOrEmail.Trim();
        var user = await userManager.FindByNameAsync(identifier)
            ?? await userManager.FindByEmailAsync(identifier);
        if (user is null)
        {
            ModelState.AddModelError(string.Empty, text["The sign-in details were not accepted."]);
            return View(model);
        }

        var result = await signInManager.PasswordSignInAsync(
            user,
            model.Password,
            model.RememberMe,
            lockoutOnFailure: true);
        if (result.Succeeded)
        {
            if (Url.IsLocalUrl(model.ReturnUrl))
            {
                return LocalRedirect(model.ReturnUrl!);
            }

            if (await userManager.IsInRoleAsync(user, "Student")
                && !await userManager.IsInRoleAsync(user, "Administrator"))
            {
                return RedirectToAction("Index", "Dashboard", new { area = "Student" });
            }

            return RedirectToAction("Index", "Home");
        }

        ModelState.AddModelError(string.Empty, text["The sign-in details were not accepted."]);
        return View(model);
    }

    [HttpPost, Authorize, ValidateAntiForgeryToken]
    public async Task<IActionResult> Logout()
    {
        await signInManager.SignOutAsync();
        return RedirectToAction("Index", "Home");
    }

    [AllowAnonymous]
    public async Task<IActionResult> SetPassword(string userId, string token)
    {
        var user = await userManager.FindByIdAsync(userId);
        if (user is null || !string.IsNullOrEmpty(user.PasswordHash))
        {
            return NotFound();
        }

        return View(new SetPasswordViewModel { UserId = userId, Token = token });
    }

    [HttpPost, AllowAnonymous, ValidateAntiForgeryToken]
    public async Task<IActionResult> SetPassword(SetPasswordViewModel model)
    {
        if (!ModelState.IsValid)
        {
            return View(model);
        }

        var user = await userManager.FindByIdAsync(model.UserId);
        if (user is null || !string.IsNullOrEmpty(user.PasswordHash))
        {
            return NotFound();
        }

        var tokenIsValid = await userManager.VerifyUserTokenAsync(
            user,
            AccountSetupTokenProviderOptions.ProviderName,
            AccountSetupTokenProviderOptions.Purpose,
            model.Token);
        if (!tokenIsValid)
        {
            ModelState.AddModelError(string.Empty, text["The account setup link is invalid or has expired. Request a new link from an administrator."]);
            return View(model);
        }

        var result = await userManager.AddPasswordAsync(user, model.Password);
        if (!result.Succeeded)
        {
            foreach (var error in result.Errors)
            {
                ModelState.AddModelError(string.Empty, GetIdentityErrorText(error));
            }

            return View(model);
        }

        return View("SetPasswordComplete");
    }

    [AllowAnonymous]
    public IActionResult AccessDenied() => View();

    private async Task<string> GetNextStudentUserNameAsync()
    {
        while (true)
        {
            var sequenceQuery = dbContext.Database
                .SqlQueryRaw<long>("SELECT NEXT VALUE FOR dbo.StudentUsernameSequence AS [Value]");
            await foreach (var number in sequenceQuery.AsAsyncEnumerable())
            {
                var userName = $"STU-{number:000000}";
                if (await userManager.FindByNameAsync(userName) is null)
                {
                    return userName;
                }
            }

            throw new InvalidOperationException("Student username sequence returned no value.");
        }
    }

    private void AddRegistrationErrors(IdentityResult result)
    {
        foreach (var error in result.Errors)
        {
            var field = error.Code switch
            {
                "DuplicateEmail" => nameof(RegisterViewModel.Email),
                "DuplicateUserName" => string.Empty,
                "InvalidEmail" => nameof(RegisterViewModel.Email),
                "PasswordTooShort" or "PasswordRequiresDigit" or "PasswordRequiresLower" or
                    "PasswordRequiresUpper" or "PasswordRequiresNonAlphanumeric" or
                    "PasswordRequiresUniqueChars" => nameof(RegisterViewModel.Password),
                _ => string.Empty
            };
            ModelState.AddModelError(field, GetIdentityErrorText(error));
        }
    }

    private string GetIdentityErrorText(IdentityError error)
    {
        var localized = text[error.Code];
        return localized.ResourceNotFound ? text["Identity validation failed."].Value : localized.Value;
    }
}
