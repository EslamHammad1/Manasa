using Manasa.Data;
using Manasa.Models;
using Manasa.ViewModels.Admin;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

namespace Manasa.Areas.Admin.Controllers;

[Area("Admin")]
[Authorize(Roles = "Administrator")]
public class CoursePackagesController(ApplicationDbContext dbContext) : Controller
{
    public async Task<IActionResult> Index(string? search, int? courseId)
    {
        var query = dbContext.CoursePackages.AsNoTracking()
            .Include(package => package.Course)
            .AsQueryable();
        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = search.Trim();
            query = query.Where(package => package.Name.Contains(term) || package.Course.Title.Contains(term));
        }
        if (courseId.HasValue)
        {
            query = query.Where(package => package.CourseId == courseId.Value);
        }

        ViewData["Search"] = search;
        ViewData["CourseId"] = courseId;
        ViewData["Courses"] = new SelectList(await dbContext.Courses.AsNoTracking().OrderBy(item => item.Title).ToListAsync(), "Id", "Title", courseId);
        return View(await query.OrderBy(package => package.Course.Title).ThenBy(package => package.Name).ToListAsync());
    }

    public async Task<IActionResult> Create()
    {
        return View("Form", await BuildFormAsync(new CoursePackageFormViewModel { AccessDurationDays = 30 }));
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(CoursePackageFormViewModel model)
    {
        await ValidateCourseAsync(model.CourseId);
        if (!ModelState.IsValid)
        {
            return View("Form", await BuildFormAsync(model));
        }

        dbContext.CoursePackages.Add(new CoursePackage
        {
            CourseId = model.CourseId,
            Name = model.Name.Trim(),
            Description = model.Description?.Trim(),
            Price = model.Price,
            AccessDurationDays = model.AccessDurationDays,
            IsActive = model.IsActive
        });
        await dbContext.SaveChangesAsync();
        TempData["StatusMessage"] = "Course package created.";
        return RedirectToAction(nameof(Index));
    }

    public async Task<IActionResult> Edit(int id)
    {
        var package = await dbContext.CoursePackages.FindAsync(id);
        if (package is null)
        {
            return NotFound();
        }

        return View("Form", await BuildFormAsync(new CoursePackageFormViewModel
        {
            Id = package.Id,
            CourseId = package.CourseId,
            Name = package.Name,
            Description = package.Description,
            Price = package.Price,
            AccessDurationDays = package.AccessDurationDays,
            IsActive = package.IsActive
        }));
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, CoursePackageFormViewModel model)
    {
        if (id != model.Id)
        {
            return BadRequest();
        }
        await ValidateCourseAsync(model.CourseId);
        if (!ModelState.IsValid)
        {
            return View("Form", await BuildFormAsync(model));
        }

        var package = await dbContext.CoursePackages.FindAsync(id);
        if (package is null)
        {
            return NotFound();
        }

        package.CourseId = model.CourseId;
        package.Name = model.Name.Trim();
        package.Description = model.Description?.Trim();
        package.Price = model.Price;
        package.AccessDurationDays = model.AccessDurationDays;
        package.IsActive = model.IsActive;
        await dbContext.SaveChangesAsync();
        TempData["StatusMessage"] = "Course package updated.";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(int id)
    {
        var package = await dbContext.CoursePackages.FindAsync(id);
        if (package is null)
        {
            return NotFound();
        }

        dbContext.CoursePackages.Remove(package);
        await dbContext.SaveChangesAsync();
        TempData["StatusMessage"] = "Course package deleted.";
        return RedirectToAction(nameof(Index));
    }

    private async Task<CoursePackageFormViewModel> BuildFormAsync(CoursePackageFormViewModel model)
    {
        model.Courses = await dbContext.Courses.AsNoTracking()
            .OrderBy(course => course.Title)
            .Select(course => new SelectListItem(course.Title, course.Id.ToString()))
            .ToListAsync();
        return model;
    }

    private async Task ValidateCourseAsync(int courseId)
    {
        if (!await dbContext.Courses.AnyAsync(course => course.Id == courseId))
        {
            ModelState.AddModelError(nameof(CoursePackageFormViewModel.CourseId), "Select a valid course.");
        }
    }
}
