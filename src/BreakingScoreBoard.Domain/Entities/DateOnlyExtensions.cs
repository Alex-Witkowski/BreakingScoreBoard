namespace BreakingScoreBoard.Domain.Entities;

/// <summary>
/// Extension methods for working with DateOnly values.
/// </summary>
public static class DateOnlyExtensions
{
    /// <summary>
    /// Calculate age at a specific event date.
    /// </summary>
    public static int GetAgeAtDate(this DateOnly birthDate, DateOnly eventDate)
    {
        var age = eventDate.Year - birthDate.Year;
        if (birthDate > eventDate.AddYears(-age))
        {
            age--;
        }
        return age;
    }
}
