using Manasa.Data;
using Manasa.Models;
using Manasa.Utilities;
using Manasa.ViewModels.Student;
using Manasa.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Manasa.Areas.Student.Controllers;

[Area("Student")]
[Authorize(Roles = "Student")]
public sealed class DashboardController(
    ApplicationDbContext dbContext,
    UserManager<ApplicationUser> userManager,
    QuizService quizService) : Controller
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

        var gamification = await quizService.GetGamificationSummaryAsync(student.Id);
        var badges = await dbContext.StudentBadges.AsNoTracking().Where(b => b.StudentId == student.Id)
            .OrderByDescending(b => b.EarnedAtUtc).Select(b => new StudentBadgeViewModel
            {
                Code = b.BadgeCode, NameResourceKey = b.Badge.NameResourceKey,
                DescriptionResourceKey = b.Badge.DescriptionResourceKey, EarnedAtUtc = b.EarnedAtUtc
            }).ToListAsync();
        var submittedQuizCount = await dbContext.QuizAttempts.AsNoTracking()
            .CountAsync(a => a.StudentId == student.Id && a.Status == QuizAttemptStatus.Submitted);
        var recentQuizzes = await dbContext.QuizAttempts.AsNoTracking()
            .Where(a => a.StudentId == student.Id && a.Status != QuizAttemptStatus.InProgress)
            .OrderByDescending(a => a.StartedAtUtc).Take(5)
            .Select(a => new StudentRecentQuizViewModel
            {
                AttemptId = a.Id, QuizId = a.QuizId, QuizTitle = a.QuizTitleSnapshot,
                Percentage = a.Percentage, Passed = a.Passed, Status = a.Status, StartedAtUtc = a.StartedAtUtc
            }).ToListAsync();
        var earnedBadgeCodes = badges.Select(b => b.Code).ToHashSet(StringComparer.Ordinal);
        var passedCourses = await dbContext.QuizAttempts.AsNoTracking()
            .Where(a => a.StudentId == student.Id && a.Passed).Select(a => a.Quiz.CourseId).Distinct().CountAsync();
        var progress = new List<AchievementProgressViewModel>();
        if (!earnedBadgeCodes.Contains("FirstQuiz")) progress.Add(new() { NameResourceKey = "Badge.FirstQuiz.Name", Current = Math.Min(submittedQuizCount, 1), Target = 1 });
        if (!earnedBadgeCodes.Contains("QuizMaster")) progress.Add(new() { NameResourceKey = "Badge.QuizMaster.Name", Current = Math.Min(passedCourses, 5), Target = 5 });
        if (!earnedBadgeCodes.Contains("LearningMilestone")) progress.Add(new() { NameResourceKey = "Badge.LearningMilestone.Name", Current = Math.Min(gamification.TotalXp, 100), Target = 100 });

        return View(new DashboardViewModel
        {
            StudentName = string.IsNullOrWhiteSpace(student.DisplayName)
                ? student.UserName ?? "Student"
                : student.DisplayName,
            Courses = courses,
            TotalXp = gamification.TotalXp,
            CurrentStreak = gamification.CurrentStreak,
            LongestStreak = gamification.LongestStreak,
            Badges = badges,
            RecentQuizzes = recentQuizzes,
            AchievementProgress = progress
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
