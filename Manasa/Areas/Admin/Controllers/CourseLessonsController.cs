using Manasa.Data;
using Manasa.Localization;
using Manasa.Models;
using Manasa.Utilities;
using Manasa.ViewModels.Admin;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Localization;

namespace Manasa.Areas.Admin.Controllers;

[Area("Admin")]
[Authorize(Roles = "Administrator")]
public sealed class CourseLessonsController(
    ApplicationDbContext dbContext,
    IStringLocalizer<SharedResource> text) : Controller
{
    public async Task<IActionResult> Index(int courseId)
    {
        var course = await dbContext.Courses.AsNoTracking()
            .Where(item => item.Id == courseId)
            .Select(item => new { item.Id, item.Title })
            .SingleOrDefaultAsync();

        if (course is null)
        {
            return NotFound();
        }

        var lessons = await dbContext.CourseLessons.AsNoTracking()
            .Where(lesson => lesson.CourseId == courseId)
            .OrderBy(lesson => lesson.DisplayOrder)
            .ThenBy(lesson => lesson.Id)
            .Select(lesson => new CourseLessonListItemViewModel
            {
                Id = lesson.Id,
                Title = lesson.Title,
                DisplayOrder = lesson.DisplayOrder,
                IsPublished = lesson.IsPublished,
                HasText = lesson.TextContent != null && lesson.TextContent != string.Empty,
                HasVideo = lesson.VideoUrl != null && lesson.VideoUrl != string.Empty,
                ProgressRecordCount = lesson.StudentProgress.Count
            })
            .ToListAsync();

        return View(new CourseLessonListViewModel
        {
            CourseId = course.Id,
            CourseTitle = course.Title,
            Lessons = lessons
        });
    }

    public async Task<IActionResult> Create(int courseId)
    {
        var course = await dbContext.Courses.AsNoTracking()
            .Where(item => item.Id == courseId)
            .Select(item => new { item.Id, item.Title })
            .SingleOrDefaultAsync();

        if (course is null)
        {
            return NotFound();
        }

        var maximumOrder = await dbContext.CourseLessons
            .Where(lesson => lesson.CourseId == courseId)
            .Select(lesson => (int?)lesson.DisplayOrder)
            .MaxAsync();

        return View("Form", new CourseLessonFormViewModel
        {
            CourseId = course.Id,
            CourseTitle = course.Title,
            DisplayOrder = (maximumOrder ?? -1) + 1
        });
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(CourseLessonFormViewModel model)
    {
        var course = await dbContext.Courses.AsNoTracking()
            .Where(item => item.Id == model.CourseId)
            .Select(item => new { item.Id, item.Title })
            .SingleOrDefaultAsync();

        if (course is null)
        {
            return NotFound();
        }

        model.CourseTitle = course.Title;
        ValidateLessonContent(model);
        if (!ModelState.IsValid)
        {
            return View("Form", model);
        }

        var lesson = new CourseLesson
        {
            CourseId = course.Id,
            Title = model.Title.Trim(),
            Description = NullIfWhiteSpace(model.Description),
            TextContent = NullIfWhiteSpace(model.TextContent),
            VideoUrl = NullIfWhiteSpace(model.VideoUrl),
            DisplayOrder = model.DisplayOrder,
            IsPublished = model.IsPublished
        };

        dbContext.CourseLessons.Add(lesson);
        await dbContext.SaveChangesAsync();

        TempData["StatusMessage"] = text["Lesson created successfully."].Value;
        return RedirectToAction(nameof(Index), new { courseId = course.Id });
    }

    public async Task<IActionResult> Edit(int courseId, int id)
    {
        var lesson = await dbContext.CourseLessons.AsNoTracking()
            .Where(item => item.CourseId == courseId && item.Id == id)
            .Select(item => new CourseLessonFormViewModel
            {
                Id = item.Id,
                CourseId = item.CourseId,
                CourseTitle = item.Course.Title,
                Title = item.Title,
                Description = item.Description,
                TextContent = item.TextContent,
                VideoUrl = item.VideoUrl,
                DisplayOrder = item.DisplayOrder,
                IsPublished = item.IsPublished
            })
            .SingleOrDefaultAsync();

        return lesson is null ? NotFound() : View("Form", lesson);
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int courseId, int id, CourseLessonFormViewModel model)
    {
        if (model.CourseId != courseId || model.Id != id)
        {
            return BadRequest();
        }

        var lesson = await dbContext.CourseLessons
            .SingleOrDefaultAsync(item => item.CourseId == courseId && item.Id == id);

        if (lesson is null)
        {
            return NotFound();
        }

        model.CourseTitle = lesson.Course.Title;
        ValidateLessonContent(model);
        if (!ModelState.IsValid)
        {
            return View("Form", model);
        }

        lesson.Title = model.Title.Trim();
        lesson.Description = NullIfWhiteSpace(model.Description);
        lesson.TextContent = NullIfWhiteSpace(model.TextContent);
        lesson.VideoUrl = NullIfWhiteSpace(model.VideoUrl);
        lesson.DisplayOrder = model.DisplayOrder;
        lesson.IsPublished = model.IsPublished;
        lesson.UpdatedAtUtc = DateTime.UtcNow;

        await dbContext.SaveChangesAsync();
        TempData["StatusMessage"] = text["Lesson saved successfully."].Value;
        return RedirectToAction(nameof(Index), new { courseId });
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(int courseId, int id)
    {
        var lesson = await dbContext.CourseLessons
            .SingleOrDefaultAsync(item => item.CourseId == courseId && item.Id == id);

        if (lesson is null)
        {
            return NotFound();
        }

        if (await dbContext.StudentLessonProgress.AnyAsync(progress => progress.LessonId == id))
        {
            TempData["ErrorMessage"] = text["This lesson has student progress and cannot be deleted. Unpublish it to remove it from the learning path."].Value;
            return RedirectToAction(nameof(Index), new { courseId });
        }

        dbContext.CourseLessons.Remove(lesson);
        await dbContext.SaveChangesAsync();
        TempData["StatusMessage"] = text["Lesson deleted successfully."].Value;
        return RedirectToAction(nameof(Index), new { courseId });
    }

    private void ValidateLessonContent(CourseLessonFormViewModel model)
    {
        if (string.IsNullOrWhiteSpace(model.TextContent) && string.IsNullOrWhiteSpace(model.VideoUrl))
        {
            ModelState.AddModelError(nameof(model.TextContent), text["Add lesson text or a supported video URL."].Value);
        }

        if (!string.IsNullOrWhiteSpace(model.VideoUrl)
            && !VideoUrlPolicy.TryGetEmbedUrl(model.VideoUrl, out _))
        {
            ModelState.AddModelError(nameof(model.VideoUrl), text["Use an HTTPS YouTube or Vimeo video URL."].Value);
        }
    }

    private static string? NullIfWhiteSpace(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}
