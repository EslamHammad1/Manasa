using Manasa.Data;
using Manasa.Models;
using Manasa.Utilities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Xunit;

namespace Manasa.Tests;

public sealed class LearningProgressTests
{
    [Theory]
    [InlineData(0, 0, 0)]
    [InlineData(0, 4, 0)]
    [InlineData(1, 4, 25)]
    [InlineData(2, 3, 67)]
    [InlineData(4, 4, 100)]
    [InlineData(8, 4, 100)]
    [InlineData(-1, 4, 0)]
    public void Percentage_handles_empty_and_clamps_counts(int completed, int total, int expected)
    {
        Assert.Equal(expected, CourseProgressCalculator.CalculatePercentage(completed, total));
    }

    [Theory]
    [InlineData("https://youtu.be/abcdefghijk", "https://www.youtube-nocookie.com/embed/abcdefghijk")]
    [InlineData("https://www.youtube.com/watch?v=abcdefghijk", "https://www.youtube-nocookie.com/embed/abcdefghijk")]
    [InlineData("https://youtube.com/shorts/abcdefghijk", "https://www.youtube-nocookie.com/embed/abcdefghijk")]
    [InlineData("https://vimeo.com/12345678", "https://player.vimeo.com/video/12345678")]
    public void Video_policy_builds_provider_embed_urls(string source, string expected)
    {
        Assert.True(VideoUrlPolicy.TryGetEmbedUrl(source, out var actual));
        Assert.Equal(expected, actual);
    }

    [Theory]
    [InlineData("http://youtu.be/abcdefghijk")]
    [InlineData("https://example.com/abcdefghijk")]
    [InlineData("https://youtube.com.evil.test/watch?v=abcdefghijk")]
    [InlineData("https://user@youtube.com/watch?v=abcdefghijk")]
    [InlineData("https://youtube.com:8443/watch?v=abcdefghijk")]
    [InlineData("javascript:alert(1)")]
    public void Video_policy_rejects_unapproved_urls(string source)
    {
        Assert.False(VideoUrlPolicy.TryGetEmbedUrl(source, out var embedUrl));
        Assert.Equal(string.Empty, embedUrl);
    }

    [Fact]
    public void Progress_model_has_unique_student_lesson_index_and_restrictive_relationships()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseSqlServer("Server=(local);Database=ModelOnly;Trusted_Connection=True;TrustServerCertificate=True")
            .Options;
        using var context = new ApplicationDbContext(options);

        var entity = context.Model.FindEntityType(typeof(StudentLessonProgress))!;
        var uniqueIndex = Assert.Single(entity.GetIndexes(), index => index.IsUnique);
        Assert.Equal(new[] { nameof(StudentLessonProgress.StudentId), nameof(StudentLessonProgress.LessonId) },
            uniqueIndex.Properties.Select(property => property.Name));

        Assert.All(entity.GetForeignKeys(), foreignKey => Assert.Equal(DeleteBehavior.Restrict, foreignKey.DeleteBehavior));
    }

    [Fact]
    public void Learning_progress_migration_generates_expected_sql_without_database_connection()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseSqlServer("Server=(local);Database=ModelOnly;Trusted_Connection=True;TrustServerCertificate=True")
            .Options;
        using var context = new ApplicationDbContext(options);

        var script = context.Database.GetService<IMigrator>().GenerateScript(
            "20261021140535_StudentAccountCreatedAt", "20261026150155_LearningProgress");

        Assert.Contains("CREATE TABLE [CourseLessons]", script, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("CREATE TABLE [StudentLessonProgress]", script, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("IX_StudentLessonProgress_StudentId_LessonId", script, StringComparison.Ordinal);
        Assert.Contains("UNIQUE", script, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("ON DELETE NO ACTION", script, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("DROP TABLE", script, StringComparison.OrdinalIgnoreCase);
    }
}
