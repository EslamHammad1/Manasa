using Manasa.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace Manasa.Data;

public static class DevelopmentDataSeeder
{
    public static async Task SeedAccountsAsync(IServiceProvider services, IConfiguration configuration)
    {
        var userManager = services.GetRequiredService<UserManager<ApplicationUser>>();
        var dbContext = services.GetRequiredService<ApplicationDbContext>();

        await EnsureAccountAsync(
            userManager,
            "DemoSeed:AdministratorPassword",
            configuration,
            "admin.demo@manasa.local",
            "admin.demo",
            "Demo Administrator",
            "Administrator");

        await EnsureAccountAsync(
            userManager,
            "DemoSeed:EslamAdministratorPassword",
            configuration,
            "eslamhammad@74",
            "eslamhammad@74",
            "Eslam Hammad",
            "Administrator");

        var teacher = await EnsureAccountAsync(
            userManager,
            "DemoSeed:TeacherPassword",
            configuration,
            "teacher.demo@manasa.local",
            "teacher.demo",
            "Demo Teacher",
            "Teacher");

        if (teacher is not null)
        {
            var profile = await dbContext.TeacherProfiles
                .SingleOrDefaultAsync(item => item.UserId == teacher.Id);

            if (profile is null)
            {
                profile = new TeacherProfile
                {
                    UserId = teacher.Id,
                    FullName = "Demo Teacher",
                    Specialization = "Programming",
                    IsActive = true,
                    CreatedAtUtc = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc)
                };
                dbContext.TeacherProfiles.Add(profile);
                await dbContext.SaveChangesAsync();
            }

            var sampleCourse = await dbContext.Courses.FindAsync(1);
            if (sampleCourse is not null && sampleCourse.TeacherProfileId is null)
            {
                sampleCourse.TeacherProfileId = profile.Id;
                await dbContext.SaveChangesAsync();
            }
        }

        await EnsureAccountAsync(
            userManager,
            "DemoSeed:StudentPassword",
            configuration,
            "student.demo@manasa.local",
            "STU-000001",
            "Demo Student",
            "Student");
    }

    private static async Task<ApplicationUser?> EnsureAccountAsync(
        UserManager<ApplicationUser> userManager,
        string passwordSetting,
        IConfiguration configuration,
        string email,
        string userName,
        string displayName,
        string role)
    {
        var password = configuration[passwordSetting];
        if (string.IsNullOrWhiteSpace(password))
        {
            return null;
        }

        var user = await userManager.FindByNameAsync(userName);
        if (user is null)
        {
            user = new ApplicationUser
            {
                UserName = userName,
                Email = email,
                EmailConfirmed = true,
                DisplayName = displayName,
                LockoutEnabled = true
            };

            var createResult = await userManager.CreateAsync(user, password);
            if (!createResult.Succeeded)
            {
                throw new InvalidOperationException(
                    $"Could not create development account {userName}: {string.Join(", ", createResult.Errors.Select(error => error.Description))}");
            }
        }

        if (!await userManager.IsInRoleAsync(user, role))
        {
            var roleResult = await userManager.AddToRoleAsync(user, role);
            if (!roleResult.Succeeded)
            {
                throw new InvalidOperationException(
                    $"Could not assign the {role} role to development account {userName}: {string.Join(", ", roleResult.Errors.Select(error => error.Description))}");
            }
        }

        return user;
    }
}
