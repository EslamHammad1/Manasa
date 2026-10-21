using Manasa.Data;
using Manasa.ViewModels.Admin;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Manasa.Areas.Admin.Controllers;

[Area("Admin")]
[Authorize(Roles = "Administrator")]
public class DashboardController(ApplicationDbContext dbContext, RoleManager<IdentityRole> roleManager) : Controller
{
    public async Task<IActionResult> Index()
    {
        var studentRole = await roleManager.FindByNameAsync("Student");
        var teacherRole = await roleManager.FindByNameAsync("Teacher");
        var model = new DashboardViewModel
        {
            StudentCount = studentRole is null ? 0 : await dbContext.UserRoles.CountAsync(item => item.RoleId == studentRole.Id),
            TeacherCount = teacherRole is null ? 0 : await dbContext.TeacherProfiles.CountAsync(item => item.IsActive),
            SubjectCount = await dbContext.Subjects.CountAsync(),
            CourseCount = await dbContext.Courses.CountAsync(),
            PublishedCourseCount = await dbContext.Courses.CountAsync(item => item.IsPublished),
            PackageCount = await dbContext.CoursePackages.CountAsync(),
            RecentCourses = await dbContext.Courses
                .AsNoTracking()
                .OrderByDescending(item => item.CreatedAtUtc)
                .Take(6)
                .Select(item => new RecentCourseViewModel
                {
                    Title = item.Title,
                    Subject = item.Subject.Name,
                    CreatedAtUtc = item.CreatedAtUtc,
                    IsPublished = item.IsPublished
                })
                .ToListAsync()
        };

        return View(model);
    }
}
