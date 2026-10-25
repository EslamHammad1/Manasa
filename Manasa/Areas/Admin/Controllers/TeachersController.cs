using Manasa.Data;
using Manasa.Localization;
using Manasa.Models;
using Manasa.ViewModels.Admin;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Localization;

namespace Manasa.Areas.Admin.Controllers;

[Area("Admin")]
[Authorize(Roles = "Administrator")]
public class TeachersController(
    ApplicationDbContext dbContext,
    UserManager<ApplicationUser> userManager,
    IStringLocalizer<SharedResource> text) : Controller
{
    private const int PageSize = 10;

    public async Task<IActionResult> Index(string? search, int page = 1)
    {
        var query = dbContext.TeacherProfiles.AsNoTracking()
            .Include(profile => profile.User)
            .Include(profile => profile.SubjectTeachers)
            .ThenInclude(link => link.Subject)
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = search.Trim();
            query = query.Where(profile => profile.FullName.Contains(term)
                || profile.User.Email!.Contains(term)
                || profile.Specialization!.Contains(term));
        }

        var totalCount = await query.CountAsync();
        var totalPages = (int)Math.Ceiling(totalCount / (double)PageSize);
        page = totalPages == 0 ? 1 : Math.Clamp(page, 1, totalPages);

        ViewData["Search"] = search;
        ViewData["PageNumber"] = page;
        ViewData["TotalPages"] = totalPages;
        return View(await query.OrderBy(profile => profile.FullName).ThenBy(profile => profile.Id)
            .Skip((page - 1) * PageSize)
            .Take(PageSize)
            .ToListAsync());
    }

    public async Task<IActionResult> Create()
    {
        return View("Form", await BuildFormAsync(new TeacherFormViewModel()));
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(TeacherFormViewModel model)
    {
        await ValidateEmailAsync(model.Email);
        await ValidateSubjectsAsync(model.SelectedSubjectIds);
        if (!ModelState.IsValid)
        {
            return View("Form", await BuildFormAsync(model));
        }

        await using var transaction = await dbContext.Database.BeginTransactionAsync();
        var teacherUser = new ApplicationUser
        {
            UserName = model.Email.Trim(),
            Email = model.Email.Trim(),
            DisplayName = model.FullName.Trim(),
            LockoutEnabled = true
        };

        var createResult = await userManager.CreateAsync(teacherUser);
        if (!createResult.Succeeded)
        {
            await transaction.RollbackAsync();
            AddErrors(createResult);
            return View("Form", await BuildFormAsync(model));
        }

        var roleResult = await userManager.AddToRoleAsync(teacherUser, "Teacher");
        if (!roleResult.Succeeded)
        {
            await transaction.RollbackAsync();
            AddErrors(roleResult);
            return View("Form", await BuildFormAsync(model));
        }

        var profile = new TeacherProfile
        {
            UserId = teacherUser.Id,
            FullName = model.FullName.Trim(),
            Biography = model.Biography?.Trim(),
            ProfileImageUrl = model.ProfileImageUrl?.Trim(),
            Specialization = model.Specialization?.Trim(),
            ExperienceYears = model.ExperienceYears,
            IsActive = model.IsActive
        };
        dbContext.TeacherProfiles.Add(profile);
        await SaveSubjectAssignmentsAsync(profile, model.SelectedSubjectIds);
        await dbContext.SaveChangesAsync();
        await transaction.CommitAsync();

        var token = await userManager.GenerateUserTokenAsync(
            teacherUser,
            AccountSetupTokenProviderOptions.ProviderName,
            AccountSetupTokenProviderOptions.Purpose);
        TempData["AccountSetupUrl"] = Url.ActionLink("SetPassword", "Account", new { userId = teacherUser.Id, token });
        TempData["StatusMessage"] = text["Teacher created. Share the one-time account setup link with the teacher."].Value;
        return RedirectToAction(nameof(Index));
    }

    public async Task<IActionResult> Edit(int id)
    {
        var profile = await dbContext.TeacherProfiles
            .Include(item => item.User)
            .Include(item => item.SubjectTeachers)
            .SingleOrDefaultAsync(item => item.Id == id);
        if (profile is null)
        {
            return NotFound();
        }

        return View("Form", await BuildFormAsync(new TeacherFormViewModel
        {
            Id = profile.Id,
            FullName = profile.FullName,
            Email = profile.User.Email ?? string.Empty,
            Biography = profile.Biography,
            ProfileImageUrl = profile.ProfileImageUrl,
            Specialization = profile.Specialization,
            ExperienceYears = profile.ExperienceYears,
            IsActive = profile.IsActive,
            SelectedSubjectIds = profile.SubjectTeachers.Select(link => link.SubjectId).ToArray()
        }));
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, TeacherFormViewModel model)
    {
        if (id != model.Id)
        {
            return BadRequest();
        }

        var profile = await dbContext.TeacherProfiles
            .Include(item => item.User)
            .Include(item => item.SubjectTeachers)
            .SingleOrDefaultAsync(item => item.Id == id);
        if (profile is null)
        {
            return NotFound();
        }

        await ValidateEmailAsync(model.Email, profile.UserId);
        await ValidateSubjectsAsync(model.SelectedSubjectIds);
        if (!ModelState.IsValid)
        {
            return View("Form", await BuildFormAsync(model));
        }

        profile.FullName = model.FullName.Trim();
        profile.Biography = model.Biography?.Trim();
        profile.ProfileImageUrl = model.ProfileImageUrl?.Trim();
        profile.Specialization = model.Specialization?.Trim();
        profile.ExperienceYears = model.ExperienceYears;
        profile.IsActive = model.IsActive;
        profile.User.DisplayName = model.FullName.Trim();
        profile.User.Email = model.Email.Trim();
        profile.User.NormalizedEmail = userManager.NormalizeEmail(profile.User.Email);
        profile.User.UserName = model.Email.Trim();
        profile.User.NormalizedUserName = userManager.NormalizeName(profile.User.UserName);

        await SaveSubjectAssignmentsAsync(profile, model.SelectedSubjectIds);
        var userResult = await userManager.UpdateAsync(profile.User);
        if (!userResult.Succeeded)
        {
            AddErrors(userResult);
            return View("Form", await BuildFormAsync(model));
        }

        await dbContext.SaveChangesAsync();
        TempData["StatusMessage"] = text["Teacher profile updated."].Value;
        return RedirectToAction(nameof(Index));
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> ToggleActive(int id)
    {
        var profile = await dbContext.TeacherProfiles.Include(item => item.User).SingleOrDefaultAsync(item => item.Id == id);
        if (profile is null)
        {
            return NotFound();
        }

        profile.IsActive = !profile.IsActive;
        await userManager.SetLockoutEnabledAsync(profile.User, true);
        await userManager.SetLockoutEndDateAsync(
            profile.User,
            profile.IsActive ? null : DateTimeOffset.UtcNow.AddYears(100));
        await dbContext.SaveChangesAsync();
        TempData["StatusMessage"] = profile.IsActive ? text["Teacher profile activated."].Value : text["Teacher profile deactivated."].Value;
        return RedirectToAction(nameof(Index));
    }

    private async Task<TeacherFormViewModel> BuildFormAsync(TeacherFormViewModel model)
    {
        model.Subjects = await dbContext.Subjects.AsNoTracking()
            .OrderBy(subject => subject.Name)
            .Select(subject => new SelectListItem(subject.Name, subject.Id.ToString()))
            .ToListAsync();
        return model;
    }

    private async Task ValidateEmailAsync(string email, string? excludeUserId = null)
    {
        var normalizedEmail = userManager.NormalizeEmail(email.Trim());
        var emailExists = await dbContext.Users.AnyAsync(user =>
            user.NormalizedEmail == normalizedEmail && user.Id != excludeUserId);
        if (emailExists)
        {
            ModelState.AddModelError(nameof(TeacherFormViewModel.Email), text["An account with this email already exists."]);
        }
    }

    private async Task ValidateSubjectsAsync(IEnumerable<int> subjectIds)
    {
        var ids = subjectIds.Distinct().ToArray();
        if (ids.Length != await dbContext.Subjects.CountAsync(subject => ids.Contains(subject.Id) && subject.IsActive))
        {
            ModelState.AddModelError(nameof(TeacherFormViewModel.SelectedSubjectIds), text["Select only active subjects."]);
        }
    }

    private async Task SaveSubjectAssignmentsAsync(TeacherProfile profile, IEnumerable<int> subjectIds)
    {
        var selected = subjectIds.Distinct().ToHashSet();
        foreach (var existing in profile.SubjectTeachers.Where(link => !selected.Contains(link.SubjectId)).ToList())
        {
            dbContext.SubjectTeachers.Remove(existing);
        }

        var existingIds = profile.SubjectTeachers.Select(link => link.SubjectId).ToHashSet();
        foreach (var subjectId in selected.Where(subjectId => !existingIds.Contains(subjectId)))
        {
            profile.SubjectTeachers.Add(new SubjectTeacher { SubjectId = subjectId, TeacherProfile = profile });
        }

        await Task.CompletedTask;
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
