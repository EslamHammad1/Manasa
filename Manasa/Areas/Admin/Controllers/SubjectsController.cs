using Manasa.Data;
using Manasa.Models;
using Manasa.ViewModels.Admin;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Manasa.Areas.Admin.Controllers;

[Area("Admin")]
[Authorize(Roles = "Administrator")]
public class SubjectsController(ApplicationDbContext dbContext) : Controller
{
    public async Task<IActionResult> Index(string? search)
    {
        IQueryable<Subject> query = dbContext.Subjects.AsNoTracking()
            .Include(item => item.Courses)
            .Include(item => item.SubjectTeachers);
        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = search.Trim();
            query = query.Where(item => item.Name.Contains(term));
        }

        ViewData["Search"] = search;
        return View(await query.OrderBy(item => item.DisplayOrder).ThenBy(item => item.Name).ToListAsync());
    }

    public IActionResult Create() => View("Form", new SubjectFormViewModel());

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(SubjectFormViewModel model)
    {
        if (await dbContext.Subjects.AnyAsync(item => item.Name == model.Name.Trim()))
        {
            ModelState.AddModelError(nameof(model.Name), "A subject with this name already exists.");
        }

        if (!ModelState.IsValid)
        {
            return View("Form", model);
        }

        dbContext.Subjects.Add(new Subject
        {
            Name = model.Name.Trim(),
            Description = model.Description?.Trim(),
            ImageUrl = model.ImageUrl?.Trim(),
            IsActive = model.IsActive,
            DisplayOrder = model.DisplayOrder
        });
        await dbContext.SaveChangesAsync();
        TempData["StatusMessage"] = "Subject created.";
        return RedirectToAction(nameof(Index));
    }

    public async Task<IActionResult> Edit(int id)
    {
        var subject = await dbContext.Subjects.FindAsync(id);
        return subject is null ? NotFound() : View("Form", new SubjectFormViewModel
        {
            Id = subject.Id,
            Name = subject.Name,
            Description = subject.Description,
            ImageUrl = subject.ImageUrl,
            IsActive = subject.IsActive,
            DisplayOrder = subject.DisplayOrder
        });
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, SubjectFormViewModel model)
    {
        if (id != model.Id)
        {
            return BadRequest();
        }

        if (await dbContext.Subjects.AnyAsync(item => item.Id != id && item.Name == model.Name.Trim()))
        {
            ModelState.AddModelError(nameof(model.Name), "A subject with this name already exists.");
        }

        if (!ModelState.IsValid)
        {
            return View("Form", model);
        }

        var subject = await dbContext.Subjects.FindAsync(id);
        if (subject is null)
        {
            return NotFound();
        }

        subject.Name = model.Name.Trim();
        subject.Description = model.Description?.Trim();
        subject.ImageUrl = model.ImageUrl?.Trim();
        subject.IsActive = model.IsActive;
        subject.DisplayOrder = model.DisplayOrder;
        await dbContext.SaveChangesAsync();
        TempData["StatusMessage"] = "Subject updated.";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(int id)
    {
        var subject = await dbContext.Subjects.FindAsync(id);
        if (subject is null)
        {
            return NotFound();
        }

        if (await dbContext.Courses.AnyAsync(item => item.SubjectId == id)
            || await dbContext.SubjectTeachers.AnyAsync(item => item.SubjectId == id))
        {
            TempData["ErrorMessage"] = "This subject is used by a course or teacher assignment. Deactivate it instead of deleting it.";
            return RedirectToAction(nameof(Index));
        }

        dbContext.Subjects.Remove(subject);
        await dbContext.SaveChangesAsync();
        TempData["StatusMessage"] = "Subject deleted.";
        return RedirectToAction(nameof(Index));
    }
}
