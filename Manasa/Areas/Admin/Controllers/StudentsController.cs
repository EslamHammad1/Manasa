using Manasa.Data;
using Manasa.Localization;
using Manasa.Models;
using Manasa.ViewModels.Admin;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Localization;

namespace Manasa.Areas.Admin.Controllers;

[Area("Admin")]
[Authorize(Roles = "Administrator")]
public class StudentsController(
    ApplicationDbContext dbContext,
    UserManager<ApplicationUser> userManager,
    IStringLocalizer<SharedResource> text) : Controller
{
    private const int PageSize = 25;

    public async Task<IActionResult> Index(string? search, int page = 1)
    {
        page = Math.Max(1, page);
        var role = await dbContext.Roles.AsNoTracking().SingleOrDefaultAsync(item => item.Name == "Student");
        var query = dbContext.Users.AsNoTracking();

        if (role is not null)
        {
            query = query.Where(user => dbContext.UserRoles.Any(link => link.UserId == user.Id && link.RoleId == role.Id));
        }
        else
        {
            query = query.Where(_ => false);
        }

        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = search.Trim();
            query = query.Where(user => user.UserName!.Contains(term)
                || (user.DisplayName != null && user.DisplayName.Contains(term))
                || (user.Email != null && user.Email.Contains(term)));
        }

        var count = await query.CountAsync();
        var items = await query.OrderBy(user => user.UserName)
            .Skip((page - 1) * PageSize)
            .Take(PageSize)
            .Select(user => new StudentListItemViewModel
            {
                UserId = user.Id,
                UserName = user.UserName ?? string.Empty,
                DisplayName = user.DisplayName,
                Email = user.Email,
                IsActive = user.LockoutEnd == null || user.LockoutEnd <= DateTimeOffset.UtcNow
            })
            .ToListAsync();

        return View(new StudentListViewModel
        {
            Search = search,
            PageNumber = page,
            TotalPages = (int)Math.Ceiling(count / (double)PageSize),
            Items = items
        });
    }

    public IActionResult Create() => View("Form", new StudentFormViewModel());

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(StudentFormViewModel model)
    {
        if (await userManager.FindByEmailAsync(model.Email.Trim()) is not null)
        {
            ModelState.AddModelError(nameof(model.Email), text["An account with this email already exists."]);
        }

        if (!ModelState.IsValid)
        {
            return View("Form", model);
        }

        await using var transaction = await dbContext.Database.BeginTransactionAsync();
        var userName = await GetNextStudentUserNameAsync();
        var student = new ApplicationUser
        {
            UserName = userName,
            Email = model.Email.Trim(),
            DisplayName = model.DisplayName.Trim(),
            LockoutEnabled = true
        };

        var createResult = await userManager.CreateAsync(student);
        if (!createResult.Succeeded)
        {
            await transaction.RollbackAsync();
            AddErrors(createResult);
            return View("Form", model);
        }

        var roleResult = await userManager.AddToRoleAsync(student, "Student");
        if (!roleResult.Succeeded)
        {
            await transaction.RollbackAsync();
            AddErrors(roleResult);
            return View("Form", model);
        }

        await transaction.CommitAsync();
        var token = await userManager.GenerateUserTokenAsync(
            student,
            AccountSetupTokenProviderOptions.ProviderName,
            AccountSetupTokenProviderOptions.Purpose);
        model.UserName = userName;
        model.AccountSetupUrl = Url.ActionLink("SetPassword", "Account", new { userId = student.Id, token });
        TempData["AccountSetupUrl"] = model.AccountSetupUrl;
        TempData["CreatedUserName"] = userName;
        return RedirectToAction(nameof(Index));
    }

    public async Task<IActionResult> Edit(string id)
    {
        var student = await FindStudentAsync(id);
        return student is null ? NotFound() : View("Form", new StudentFormViewModel
        {
            UserId = student.Id,
            UserName = student.UserName,
            DisplayName = student.DisplayName ?? string.Empty,
            Email = student.Email ?? string.Empty
        });
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(string id, StudentFormViewModel model)
    {
        if (id != model.UserId)
        {
            return BadRequest();
        }

        var student = await FindStudentAsync(id);
        if (student is null)
        {
            return NotFound();
        }

        var emailOwner = await userManager.FindByEmailAsync(model.Email.Trim());
        if (emailOwner is not null && emailOwner.Id != student.Id)
        {
            ModelState.AddModelError(nameof(model.Email), text["An account with this email already exists."]);
        }

        if (!ModelState.IsValid)
        {
            model.UserName = student.UserName;
            return View("Form", model);
        }

        student.DisplayName = model.DisplayName.Trim();
        student.Email = model.Email.Trim();
        student.NormalizedEmail = userManager.NormalizeEmail(student.Email);
        var result = await userManager.UpdateAsync(student);
        if (!result.Succeeded)
        {
            AddErrors(result);
            model.UserName = student.UserName;
            return View("Form", model);
        }

        TempData["StatusMessage"] = text["Student account updated."].Value;
        return RedirectToAction(nameof(Index));
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> ToggleActive(string id, int page = 1, string? search = null)
    {
        var student = await FindStudentAsync(id);
        if (student is null)
        {
            return NotFound();
        }

        await userManager.SetLockoutEnabledAsync(student, true);
        var isActive = student.LockoutEnd is null || student.LockoutEnd <= DateTimeOffset.UtcNow;
        await userManager.SetLockoutEndDateAsync(
            student,
            isActive ? DateTimeOffset.UtcNow.AddYears(100) : null);

        TempData["StatusMessage"] = isActive ? text["Student account deactivated."].Value : text["Student account activated."].Value;
        return RedirectToAction(nameof(Index), new { page, search });
    }

    private async Task<string> GetNextStudentUserNameAsync()
    {
        while (true)
        {
            long? number = null;
            var sequenceQuery = dbContext.Database
                .SqlQueryRaw<long>("SELECT NEXT VALUE FOR dbo.StudentUsernameSequence AS [Value]");
            await foreach (var value in sequenceQuery.AsAsyncEnumerable())
            {
                number = value;
                break;
            }

            if (number is null)
            {
                throw new InvalidOperationException("Student username sequence returned no value.");
            }

            var userName = $"STU-{number:000000}";
            if (await userManager.FindByNameAsync(userName) is null)
            {
                return userName;
            }
        }
    }

    private Task<ApplicationUser?> FindStudentAsync(string id)
    {
        return dbContext.Users
            .Where(user => user.Id == id && dbContext.UserRoles.Any(link =>
                link.UserId == user.Id && dbContext.Roles.Any(role => role.Id == link.RoleId && role.Name == "Student")))
            .SingleOrDefaultAsync();
    }

    private void AddErrors(IdentityResult result)
    {
        foreach (var error in result.Errors)
        {
            var localized = text[error.Code];
            ModelState.AddModelError(string.Empty, localized.ResourceNotFound ? text["Identity validation failed."].Value : localized.Value);
        }
    }
}
