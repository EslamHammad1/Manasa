using Manasa.Data;
using Manasa.Models;
using Manasa.Utilities;
using Manasa.ViewModels.Student;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Manasa.Areas.Student.Controllers;

[Area("Student")]
[Authorize(Roles = "Student")]
public sealed class LessonsController(
    ApplicationDbContext dbContext,
    UserManager<ApplicationUser> userManager) : Controller
{
    [HttpGet]
    public async Task<IActionResult> Details(int courseId, int lessonId)
    {
        var student = await userManager.GetUserAsync(User);
        if (student is null)
        {
            return Challenge();
        }

        var lesson = await dbContext.CourseLessons
            .AsNoTracking()
            .Where(item => item.Id == lessonId && item.CourseId == courseId && item.IsPublished && item.Course.IsPublished)
            .Select(item => new
            {
                item.Id,
                item.CourseId,
                CourseTitle = item.Course.Title,
                item.Title,
                item.Description,
                item.TextContent,
                item.VideoUrl
            })
            .SingleOrDefaultAsync();

        if (lesson is null)
        {
            return NotFound();
        }

        var lessons = await dbContext.CourseLessons
            .AsNoTracking()
            .Where(item => item.CourseId == courseId && item.IsPublished)
            .OrderBy(item => item.DisplayOrder).ThenBy(item => item.Id)
            .Select(item => new
            {
                item.Id,
                item.Title,
                item.DisplayOrder,
                IsCompleted = item.StudentProgress.Any(progress => progress.StudentId == student.Id && progress.IsCompleted),
                LastAccessedAtUtc = item.StudentProgress
                    .Where(progress => progress.StudentId == student.Id)
                    .Select(progress => progress.LastAccessedAtUtc)
                    .FirstOrDefault()
            })
            .ToListAsync();

        var currentIndex = lessons.FindIndex(item => item.Id == lessonId);
        if (currentIndex < 0)
        {
            return NotFound();
        }

        VideoUrlPolicy.TryGetEmbedUrl(lesson.VideoUrl, out var embedUrl);
        var completedCount = lessons.Count(item => item.IsCompleted);

        return View(new LessonLearningViewModel
        {
            CourseId = lesson.CourseId,
            CourseTitle = lesson.CourseTitle,
            LessonId = lesson.Id,
            Title = lesson.Title,
            Description = lesson.Description,
            TextContent = lesson.TextContent,
            VideoEmbedUrl = string.IsNullOrEmpty(embedUrl) ? null : embedUrl,
            OriginalVideoUrl = string.IsNullOrEmpty(embedUrl) ? null : lesson.VideoUrl,
            IsCompleted = lessons[currentIndex].IsCompleted,
            LessonNumber = currentIndex + 1,
            CompletedLessons = completedCount,
            TotalLessons = lessons.Count,
            CompletionPercentage = CourseProgressCalculator.CalculatePercentage(completedCount, lessons.Count),
            PreviousLessonId = currentIndex > 0 ? lessons[currentIndex - 1].Id : null,
            NextLessonId = currentIndex + 1 < lessons.Count ? lessons[currentIndex + 1].Id : null,
            Lessons = lessons.Select(item => new LessonNavigationItemViewModel
            {
                Id = item.Id,
                Title = item.Title,
                DisplayOrder = item.DisplayOrder,
                IsCompleted = item.IsCompleted,
                IsCurrent = item.Id == lessonId
            }).ToList()
        });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Open(int courseId, int lessonId)
    {
        var student = await userManager.GetUserAsync(User);
        if (student is null)
        {
            return Challenge();
        }

        if (!await IsAvailableLesson(courseId, lessonId))
        {
            return NotFound();
        }

        var progress = await dbContext.StudentLessonProgress
            .SingleOrDefaultAsync(item => item.StudentId == student.Id && item.LessonId == lessonId);
        var now = DateTime.UtcNow;
        if (progress is null)
        {
            progress = new StudentLessonProgress
            {
                StudentId = student.Id,
                LessonId = lessonId,
                LastAccessedAtUtc = now
            };
            dbContext.StudentLessonProgress.Add(progress);
        }
        else
        {
            progress.LastAccessedAtUtc = now;
        }

        try
        {
            await dbContext.SaveChangesAsync();
        }
        catch (DbUpdateException) when (dbContext.Entry(progress).State == EntityState.Added)
        {
            dbContext.Entry(progress).State = EntityState.Detached;
            var winner = await dbContext.StudentLessonProgress
                .SingleOrDefaultAsync(item => item.StudentId == student.Id && item.LessonId == lessonId);
            if (winner is null)
            {
                throw;
            }

            winner.LastAccessedAtUtc = now;
            await dbContext.SaveChangesAsync();
        }

        return RedirectToAction(nameof(Details), new { courseId, lessonId });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Complete(int courseId, int lessonId)
    {
        var student = await userManager.GetUserAsync(User);
        if (student is null)
        {
            return Challenge();
        }

        if (!await IsAvailableLesson(courseId, lessonId))
        {
            return NotFound();
        }

        var now = DateTime.UtcNow;
        var progress = await dbContext.StudentLessonProgress
            .SingleOrDefaultAsync(item => item.StudentId == student.Id && item.LessonId == lessonId);
        if (progress is null)
        {
            progress = new StudentLessonProgress
            {
                StudentId = student.Id,
                LessonId = lessonId,
                IsCompleted = true,
                CompletedAtUtc = now,
                LastAccessedAtUtc = now
            };
            dbContext.StudentLessonProgress.Add(progress);
        }
        else
        {
            progress.LastAccessedAtUtc = now;
            if (!progress.IsCompleted)
            {
                progress.IsCompleted = true;
                progress.CompletedAtUtc = now;
            }
        }

        try
        {
            await dbContext.SaveChangesAsync();
        }
        catch (DbUpdateException) when (dbContext.Entry(progress).State == EntityState.Added)
        {
            dbContext.Entry(progress).State = EntityState.Detached;
            var winner = await dbContext.StudentLessonProgress
                .SingleOrDefaultAsync(item => item.StudentId == student.Id && item.LessonId == lessonId);
            if (winner is null)
            {
                throw;
            }

            winner.LastAccessedAtUtc = now;
            if (!winner.IsCompleted)
            {
                winner.IsCompleted = true;
                winner.CompletedAtUtc = now;
            }

            await dbContext.SaveChangesAsync();
        }

        return RedirectToAction(nameof(Details), new { courseId, lessonId });
    }

    private Task<bool> IsAvailableLesson(int courseId, int lessonId) => dbContext.CourseLessons
        .AsNoTracking()
        .AnyAsync(item => item.Id == lessonId && item.CourseId == courseId && item.IsPublished && item.Course.IsPublished);
}
