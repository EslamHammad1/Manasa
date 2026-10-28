namespace Manasa.Utilities;

public static class CourseProgressCalculator
{
    public static int CalculatePercentage(int completedLessons, int totalLessons)
    {
        if (totalLessons <= 0)
        {
            return 0;
        }

        var completed = Math.Clamp(completedLessons, 0, totalLessons);
        return (int)Math.Round(completed * 100d / totalLessons, MidpointRounding.AwayFromZero);
    }
}
