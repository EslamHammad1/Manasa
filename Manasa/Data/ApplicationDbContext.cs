using Manasa.Models;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace Manasa.Data;

public class ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
    : IdentityDbContext<ApplicationUser>(options)
{
    public DbSet<Subject> Subjects => Set<Subject>();

    public DbSet<TeacherProfile> TeacherProfiles => Set<TeacherProfile>();

    public DbSet<SubjectTeacher> SubjectTeachers => Set<SubjectTeacher>();

    public DbSet<Course> Courses => Set<Course>();

    public DbSet<CoursePackage> CoursePackages => Set<CoursePackage>();

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        builder.HasSequence<long>("StudentUsernameSequence");

        builder.Entity<ApplicationUser>()
            .HasIndex(user => user.NormalizedEmail)
            .IsUnique()
            .HasFilter("[NormalizedEmail] IS NOT NULL");

        builder.Entity<ApplicationUser>()
            .Property(user => user.CreatedAtUtc)
            .HasDefaultValueSql("SYSUTCDATETIME()");

        builder.Entity<Microsoft.AspNetCore.Identity.IdentityRole>().HasData(
            new Microsoft.AspNetCore.Identity.IdentityRole
            {
                Id = "a1000000-0000-0000-0000-000000000001",
                Name = "Administrator",
                NormalizedName = "ADMINISTRATOR",
                ConcurrencyStamp = "a1000000-0000-0000-0000-000000000001"
            },
            new Microsoft.AspNetCore.Identity.IdentityRole
            {
                Id = "a1000000-0000-0000-0000-000000000002",
                Name = "Teacher",
                NormalizedName = "TEACHER",
                ConcurrencyStamp = "a1000000-0000-0000-0000-000000000002"
            },
            new Microsoft.AspNetCore.Identity.IdentityRole
            {
                Id = "a1000000-0000-0000-0000-000000000003",
                Name = "Student",
                NormalizedName = "STUDENT",
                ConcurrencyStamp = "a1000000-0000-0000-0000-000000000003"
            });

        builder.Entity<Subject>()
            .HasIndex(subject => subject.Name)
            .IsUnique();

        builder.Entity<Subject>()
            .Property(subject => subject.CreatedAtUtc)
            .HasDefaultValueSql("SYSUTCDATETIME()");

        builder.Entity<TeacherProfile>()
            .HasIndex(profile => profile.UserId)
            .IsUnique();

        builder.Entity<TeacherProfile>()
            .Property(profile => profile.CreatedAtUtc)
            .HasDefaultValueSql("SYSUTCDATETIME()");

        builder.Entity<TeacherProfile>()
            .HasOne(profile => profile.User)
            .WithOne()
            .HasForeignKey<TeacherProfile>(profile => profile.UserId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.Entity<SubjectTeacher>()
            .HasKey(link => new { link.SubjectId, link.TeacherProfileId });

        builder.Entity<SubjectTeacher>()
            .HasOne(link => link.Subject)
            .WithMany(subject => subject.SubjectTeachers)
            .HasForeignKey(link => link.SubjectId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.Entity<SubjectTeacher>()
            .HasOne(link => link.TeacherProfile)
            .WithMany(profile => profile.SubjectTeachers)
            .HasForeignKey(link => link.TeacherProfileId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.Entity<Course>()
            .Property(course => course.CreatedAtUtc)
            .HasDefaultValueSql("SYSUTCDATETIME()");

        builder.Entity<Course>()
            .HasOne(course => course.Subject)
            .WithMany(subject => subject.Courses)
            .HasForeignKey(course => course.SubjectId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.Entity<Course>()
            .HasOne(course => course.TeacherProfile)
            .WithMany(profile => profile.Courses)
            .HasForeignKey(course => course.TeacherProfileId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.Entity<CoursePackage>()
            .Property(package => package.Price)
            .HasPrecision(18, 2);

        builder.Entity<CoursePackage>()
            .Property(package => package.CreatedAtUtc)
            .HasDefaultValueSql("SYSUTCDATETIME()");

        builder.Entity<CoursePackage>()
            .HasOne(package => package.Course)
            .WithMany(course => course.Packages)
            .HasForeignKey(package => package.CourseId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.Entity<Subject>().HasData(
            new Subject { Id = 1, Name = "Programming", Description = "Foundations of software development.", IsActive = true, DisplayOrder = 1, CreatedAtUtc = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc) },
            new Subject { Id = 2, Name = "Mathematics", Description = "Core mathematics courses.", IsActive = true, DisplayOrder = 2, CreatedAtUtc = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc) });

        builder.Entity<Course>().HasData(
            new Course { Id = 1, SubjectId = 1, Title = "Introduction to Programming", Description = "A sample course used to verify the initial catalog schema.", IsPublished = false, CreatedAtUtc = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc) });

        builder.Entity<CoursePackage>().HasData(
            new CoursePackage { Id = 1, CourseId = 1, Name = "Standard", Description = "Sample package for local development.", Price = 0m, AccessDurationDays = 30, IsActive = true, CreatedAtUtc = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc) });
    }
}
