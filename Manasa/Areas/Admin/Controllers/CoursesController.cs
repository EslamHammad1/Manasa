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
public class CoursesController(ApplicationDbContext dbContext) : Controller
{
    public async Task<IActionResult> Index(string? search, int? subjectId, bool? published)
    {
        var query = dbContext.Courses.AsNoTracking()
            .Include(course => course.Subject)
            .Include(course => course.TeacherProfile)
            .Include(course => course.Packages)
            .AsQueryable();
        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = search.Trim();
            query = query.Where(course => course.Title.Contains(term));
        }
        if (subjectId.HasValue)
        {
            query = query.Where(course => course.SubjectId == subjectId.Value);
        }
        if (published.HasValue)
        {
            query = query.Where(course => course.IsPublished == published.Value);
        }

        ViewData["Search"] = search;
        ViewData["SubjectId"] = subjectId;
        ViewData["Published"] = published;
        ViewData["Subjects"] = new SelectList(await dbContext.Subjects.AsNoTracking().OrderBy(item => item.Name).ToListAsync(), "Id", "Name", subjectId);
        return View(await query.OrderBy(course => course.Title).ToListAsync());
    }

    public async Task<IActionResult> Create()
    {
        return View("Form", await BuildFormAsync(new CourseFormViewModel()));
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(CourseFormViewModel model)
    {
        await ValidateCourseLinksAsync(model);
        if (!ModelState.IsValid)
        {
            return View("Form", await BuildFormAsync(model));
        }

        dbContext.Courses.Add(new Course
        {
            SubjectId = model.SubjectId,
            TeacherProfileId = model.TeacherProfileId,
            Title = model.Title.Trim(),
            Description = model.Description?.Trim(),
            ThumbnailUrl = model.ThumbnailUrl?.Trim(),
            IsPublished = model.IsPublished
        });
        await dbContext.SaveChangesAsync();
        TempData["StatusMessage"] = "Course created.";
        return RedirectToAction(nameof(Index));
    }

    public async Task<IActionResult> Edit(int id)
    {
        var course = await dbContext.Courses.FindAsync(id);
        if (course is null)
        {
            return NotFound();
        }

        return View("Form", await BuildFormAsync(new CourseFormViewModel
        {
            Id = course.Id,
            SubjectId = course.SubjectId,
            TeacherProfileId = course.TeacherProfileId,
            Title = course.Title,
            Description = course.Description,
            ThumbnailUrl = course.ThumbnailUrl,
            IsPublished = course.IsPublished
        }));
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, CourseFormViewModel model)
    {
        if (id != model.Id)
        {
            return BadRequest();
        }
        await ValidateCourseLinksAsync(model);
        if (!ModelState.IsValid)
        {
            return View("Form", await BuildFormAsync(model));
        }

        var course = await dbContext.Courses.FindAsync(id);
        if (course is null)
        {
            return NotFound();
        }

        course.SubjectId = model.SubjectId;
        course.TeacherProfileId = model.TeacherProfileId;
        course.Title = model.Title.Trim();
        course.Description = model.Description?.Trim();
        course.ThumbnailUrl = model.ThumbnailUrl?.Trim();
        course.IsPublished = model.IsPublished;
        await dbContext.SaveChangesAsync();
        TempData["StatusMessage"] = "Course updated.";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(int id)
    {
        var course = await dbContext.Courses.FindAsync(id);
        if (course is null)
        {
            return NotFound();
        }
        if (await dbContext.CoursePackages.AnyAsync(item => item.CourseId == id)
            || await dbContext.Courses.AnyAsync(item => item.Id == id && item.TeacherProfileId != null))
        {
            TempData["ErrorMessage"] = "This course still has packages or an assigned teacher. Remove or reassign those references before deleting it.";
            return RedirectToAction(nameof(Index));
        }

        dbContext.Courses.Remove(course);
        await dbContext.SaveChangesAsync();
        TempData["StatusMessage"] = "Course deleted.";
        return RedirectToAction(nameof(Index));
    }

    private async Task<CourseFormViewModel> BuildFormAsync(CourseFormViewModel model)
    {
        model.Subjects = await dbContext.Subjects.AsNoTracking()
            .Where(subject => subject.IsActive || subject.Id == model.SubjectId)
            .OrderBy(subject => subject.Name)
            .Select(subject => new SelectListItem(subject.Name, subject.Id.ToString()))
            .ToListAsync();
        model.Teachers = await dbContext.TeacherProfiles.AsNoTracking()
            .Where(profile => profile.IsActive || profile.Id == model.TeacherProfileId)
            .OrderBy(profile => profile.FullName)
            .Select(profile => new SelectListItem(profile.FullName, profile.Id.ToString()))
            .ToListAsync();
        return model;
    }

    private async Task ValidateCourseLinksAsync(CourseFormViewModel model)
    {
        var existingCourse = model.Id == 0 ? null : await dbContext.Courses.AsNoTracking().SingleOrDefaultAsync(course => course.Id == model.Id);
        if (!await dbContext.Subjects.AnyAsync(subject => subject.Id == model.SubjectId
            && (subject.IsActive || (existingCourse != null && existingCourse.SubjectId == subject.Id))))
        {
            ModelState.AddModelError(nameof(model.SubjectId), "Select an active subject.");
        }

        if (model.TeacherProfileId.HasValue
            && !await dbContext.SubjectTeachers.AnyAsync(link =>
                link.SubjectId == model.SubjectId
                && link.TeacherProfileId == model.TeacherProfileId
                && (link.TeacherProfile.IsActive
                    || (existingCourse != null && existingCourse.TeacherProfileId == model.TeacherProfileId))))
        {
            ModelState.AddModelError(nameof(model.TeacherProfileId), "Assign the selected teacher to this subject first.");
        }
    }
}
