using Manasa.Data;
using Manasa.Models;
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
            .Select(course => new PublishedCourseViewModel
            {
                Id = course.Id,
                Title = course.Title,
                Subject = course.Subject.Name,
                Description = course.Description,
                ThumbnailUrl = course.ThumbnailUrl
            })
            .ToListAsync();

        return View(new DashboardViewModel
        {
            StudentName = string.IsNullOrWhiteSpace(student.DisplayName)
                ? student.UserName ?? "Student"
                : student.DisplayName,
            Courses = courses
        });
    }

    public async Task<IActionResult> Details(int id)
    {
        var course = await dbContext.Courses
            .AsNoTracking()
            .Where(course => course.Id == id && course.IsPublished)
            .Select(course => new CourseDetailsViewModel
            {
                Title = course.Title,
                Subject = course.Subject.Name,
                Description = course.Description,
                ThumbnailUrl = course.ThumbnailUrl,
                TeacherName = course.TeacherProfile == null ? null : course.TeacherProfile.FullName
            })
            .SingleOrDefaultAsync();

        return course is null ? NotFound() : View(course);
    }
}
