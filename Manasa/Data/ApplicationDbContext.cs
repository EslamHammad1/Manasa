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

    public DbSet<Quiz> Quizzes => Set<Quiz>();
    public DbSet<QuizQuestion> QuizQuestions => Set<QuizQuestion>();
    public DbSet<QuizOption> QuizOptions => Set<QuizOption>();
    public DbSet<QuizAttempt> QuizAttempts => Set<QuizAttempt>();
    public DbSet<QuizAttemptAnswer> QuizAttemptAnswers => Set<QuizAttemptAnswer>();
    public DbSet<StudentXpAward> StudentXpAwards => Set<StudentXpAward>();
    public DbSet<BadgeDefinition> BadgeDefinitions => Set<BadgeDefinition>();
    public DbSet<StudentBadge> StudentBadges => Set<StudentBadge>();
    public DbSet<StudentLearningDay> StudentLearningDays => Set<StudentLearningDay>();

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

        builder.Entity<Quiz>()
            .HasOne(quiz => quiz.Course).WithMany().HasForeignKey(quiz => quiz.CourseId)
            .OnDelete(DeleteBehavior.Restrict);
        builder.Entity<Quiz>().HasIndex(quiz => new { quiz.CourseId, quiz.IsPublished });
        builder.Entity<Quiz>().Property(quiz => quiz.PassingScorePercent).HasPrecision(5, 2);
        builder.Entity<Quiz>().Property(quiz => quiz.CreatedAtUtc).HasDefaultValueSql("SYSUTCDATETIME()");
        builder.Entity<Quiz>().Property(quiz => quiz.UpdatedAtUtc).HasDefaultValueSql("SYSUTCDATETIME()");
        builder.Entity<QuizQuestion>().Property(question => question.Points).HasPrecision(8, 2);
        builder.Entity<QuizQuestion>().HasIndex(question => new { question.QuizId, question.DisplayOrder });
        builder.Entity<QuizQuestion>().HasOne(question => question.Quiz).WithMany(quiz => quiz.Questions)
            .HasForeignKey(question => question.QuizId).OnDelete(DeleteBehavior.Cascade);
        builder.Entity<QuizOption>().HasIndex(option => new { option.QuizQuestionId, option.DisplayOrder });
        builder.Entity<QuizOption>().HasOne(option => option.QuizQuestion).WithMany(question => question.Options)
            .HasForeignKey(option => option.QuizQuestionId).OnDelete(DeleteBehavior.Cascade);
        builder.Entity<QuizAttempt>().Property(attempt => attempt.EarnedScore).HasPrecision(10, 2);
        builder.Entity<QuizAttempt>().Property(attempt => attempt.MaximumScore).HasPrecision(10, 2);
        builder.Entity<QuizAttempt>().Property(attempt => attempt.Percentage).HasPrecision(5, 2);
        builder.Entity<QuizAttempt>().Property(attempt => attempt.PassingScorePercentSnapshot).HasPrecision(5, 2);
        builder.Entity<QuizAttempt>().Property(attempt => attempt.RowVersion).IsRowVersion();
        builder.Entity<QuizAttempt>().HasIndex(attempt => new { attempt.StudentId, attempt.QuizId, attempt.AttemptNumber }).IsUnique();
        builder.Entity<QuizAttempt>().HasIndex(attempt => new { attempt.StudentId, attempt.QuizId, attempt.Status });
        builder.Entity<QuizAttempt>().HasOne(attempt => attempt.Student).WithMany()
            .HasForeignKey(attempt => attempt.StudentId).OnDelete(DeleteBehavior.Restrict);
        builder.Entity<QuizAttempt>().HasOne(attempt => attempt.Quiz).WithMany(quiz => quiz.Attempts)
            .HasForeignKey(attempt => attempt.QuizId).OnDelete(DeleteBehavior.Restrict);
        builder.Entity<QuizAttemptAnswer>().Property(answer => answer.EarnedPoints).HasPrecision(8, 2);
        builder.Entity<QuizAttemptAnswer>().Property(answer => answer.MaximumPoints).HasPrecision(8, 2);
        builder.Entity<QuizAttemptAnswer>().HasIndex(answer => new { answer.QuizAttemptId, answer.QuestionId }).IsUnique();
        builder.Entity<QuizAttemptAnswer>().HasOne(answer => answer.Attempt).WithMany(attempt => attempt.Answers)
            .HasForeignKey(answer => answer.QuizAttemptId).OnDelete(DeleteBehavior.Cascade);
        builder.Entity<StudentXpAward>().HasIndex(award => new { award.StudentId, award.EventType, award.EventKey }).IsUnique();
        builder.Entity<StudentXpAward>().HasOne(award => award.Student).WithMany()
            .HasForeignKey(award => award.StudentId).OnDelete(DeleteBehavior.Restrict);
        builder.Entity<BadgeDefinition>().HasKey(badge => badge.Code);
        builder.Entity<StudentBadge>().HasIndex(badge => new { badge.StudentId, badge.BadgeCode }).IsUnique();
        builder.Entity<StudentBadge>().HasOne(badge => badge.Student).WithMany()
            .HasForeignKey(badge => badge.StudentId).OnDelete(DeleteBehavior.Restrict);
        builder.Entity<StudentBadge>().HasOne(badge => badge.Badge).WithMany()
            .HasForeignKey(badge => badge.BadgeCode).OnDelete(DeleteBehavior.Restrict);
        builder.Entity<StudentLearningDay>().HasIndex(day => new { day.StudentId, day.ActivityDate }).IsUnique();
        builder.Entity<StudentLearningDay>().HasOne(day => day.Student).WithMany()
            .HasForeignKey(day => day.StudentId).OnDelete(DeleteBehavior.Restrict);

        builder.Entity<BadgeDefinition>().HasData(
            new BadgeDefinition { Code = "FirstQuiz", NameResourceKey = "Badge.FirstQuiz.Name", DescriptionResourceKey = "Badge.FirstQuiz.Description", IsActive = true },
            new BadgeDefinition { Code = "QuizMaster", NameResourceKey = "Badge.QuizMaster.Name", DescriptionResourceKey = "Badge.QuizMaster.Description", IsActive = true },
            new BadgeDefinition { Code = "PerfectScore", NameResourceKey = "Badge.PerfectScore.Name", DescriptionResourceKey = "Badge.PerfectScore.Description", IsActive = true },
            new BadgeDefinition { Code = "LearningMilestone", NameResourceKey = "Badge.LearningMilestone.Name", DescriptionResourceKey = "Badge.LearningMilestone.Description", IsActive = true });

        builder.Entity<Subject>().HasData(
            new Subject { Id = 1, Name = "Programming", Description = "Foundations of software development.", IsActive = true, DisplayOrder = 1, CreatedAtUtc = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc) },
            new Subject { Id = 2, Name = "Mathematics", Description = "Core mathematics courses.", IsActive = true, DisplayOrder = 2, CreatedAtUtc = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc) });

        builder.Entity<Course>().HasData(
            new Course { Id = 1, SubjectId = 1, Title = "Introduction to Programming", Description = "A sample course used to verify the initial catalog schema.", IsPublished = false, CreatedAtUtc = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc) });

        builder.Entity<CoursePackage>().HasData(
            new CoursePackage { Id = 1, CourseId = 1, Name = "Standard", Description = "Sample package for local development.", Price = 0m, AccessDurationDays = 30, IsActive = true, CreatedAtUtc = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc) });
    }
}
