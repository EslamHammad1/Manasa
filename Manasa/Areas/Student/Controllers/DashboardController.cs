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
public sealed class DashboardController(
    ApplicationDbContext dbContext,
    UserManager<ApplicationUser> userManager) : Controller
{
    public async Task<IActionResult> Index()
    {
        var student = await userManager.GetUserAsync(User);
        if (student is null)
        {
            return Challenge();
        }

        var courses = await dbContext.Courses
            .AsNoTracking()
            .Where(course => course.IsPublished)
            .OrderBy(course => course.Title)
            .Select(course => new
            {
                Course = new PublishedCourseViewModel
                {
                    Id = course.Id,
                    Title = course.Title,
                    Subject = course.Subject.Name,
                    Description = course.Description,
                    ThumbnailUrl = course.ThumbnailUrl,
                    TotalLessons = course.Lessons.Count(lesson => lesson.IsPublished),
                    CompletedLessons = course.Lessons.Count(lesson => lesson.IsPublished
                        && lesson.StudentProgress.Any(progress => progress.StudentId == student.Id && progress.IsCompleted)),
                    ResumeLessonId = course.Lessons.Where(lesson => lesson.IsPublished)
                        .OrderBy(lesson => lesson.DisplayOrder).ThenBy(lesson => lesson.Id)
                        .Select(lesson => (int?)lesson.Id).FirstOrDefault()
                },
                FirstLessonId = course.Lessons.Where(lesson => lesson.IsPublished)
                    .OrderBy(lesson => lesson.DisplayOrder).ThenBy(lesson => lesson.Id)
                    .Select(lesson => (int?)lesson.Id).FirstOrDefault()
            })
            .ToListAsync();

        var accessedLessons = await dbContext.StudentLessonProgress
            .AsNoTracking()
            .Where(progress => progress.StudentId == student.Id
                && progress.LastAccessedAtUtc != null
                && progress.Lesson.IsPublished
                && progress.Lesson.Course.IsPublished)
            .OrderByDescending(progress => progress.LastAccessedAtUtc)
            .ThenBy(progress => progress.Lesson.DisplayOrder)
            .ThenBy(progress => progress.LessonId)
            .Select(progress => new { progress.Lesson.CourseId, progress.LessonId })
            .ToListAsync();

        var latestAccessByCourse = accessedLessons
            .GroupBy(item => item.CourseId)
            .ToDictionary(group => group.Key, group => group.First().LessonId);

        foreach (var result in courses)
        {
            result.Course.CompletionPercentage = CourseProgressCalculator.CalculatePercentage(
                result.Course.CompletedLessons, result.Course.TotalLessons);
            result.Course.ResumeLessonId = latestAccessByCourse.TryGetValue(result.Course.Id, out var lastAccessedLessonId)
                ? lastAccessedLessonId
                : result.FirstLessonId;
        }

        return View(new DashboardViewModel
        {
            StudentName = string.IsNullOrWhiteSpace(student.DisplayName)
                ? student.UserName ?? "Student"
                : student.DisplayName,
            Courses = courses.Select(result => result.Course).ToList()
        });
    }

    public async Task<IActionResult> Details(int id)
    {
        var student = await userManager.GetUserAsync(User);
        if (student is null)
        {
            return Challenge();
        }

        var course = await dbContext.Courses
            .AsNoTracking()
            .Where(course => course.Id == id && course.IsPublished)
            .Select(course => new CourseDetailsViewModel
            {
                Id = course.Id,
                Title = course.Title,
                Subject = course.Subject.Name,
                Description = course.Description,
                ThumbnailUrl = course.ThumbnailUrl,
                TeacherName = course.TeacherProfile == null ? null : course.TeacherProfile.FullName
            })
            .SingleOrDefaultAsync();

        if (course is null)
        {
            return NotFound();
        }

        var lessons = await dbContext.CourseLessons
            .AsNoTracking()
            .Where(lesson => lesson.CourseId == id && lesson.IsPublished)
            .OrderBy(lesson => lesson.DisplayOrder).ThenBy(lesson => lesson.Id)
            .Select(lesson => new CourseLessonSummaryViewModel
            {
                Id = lesson.Id,
                Title = lesson.Title,
                DisplayOrder = lesson.DisplayOrder,
                IsCompleted = lesson.StudentProgress.Any(progress => progress.StudentId == student.Id && progress.IsCompleted),
                LastAccessedAtUtc = lesson.StudentProgress
                    .Where(progress => progress.StudentId == student.Id)
                    .Select(progress => progress.LastAccessedAtUtc)
                    .FirstOrDefault()
            })
            .ToListAsync();

        course.Lessons = lessons;
        course.TotalLessons = lessons.Count;
        course.CompletedLessons = lessons.Count(lesson => lesson.IsCompleted);
        course.CompletionPercentage = CourseProgressCalculator.CalculatePercentage(
            course.CompletedLessons, course.TotalLessons);
        course.ResumeLessonId = lessons
            .Where(lesson => lesson.LastAccessedAtUtc.HasValue)
            .OrderByDescending(lesson => lesson.LastAccessedAtUtc)
            .ThenBy(lesson => lesson.DisplayOrder).ThenBy(lesson => lesson.Id)
            .Select(lesson => (int?)lesson.Id)
            .FirstOrDefault()
            ?? lessons.FirstOrDefault()?.Id;

        return View(course);
    }
}
