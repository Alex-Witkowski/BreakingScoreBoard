namespace BreakingScoreBoard.Domain.Entities;

/// <summary>
/// Represents a participant who competes in battles.
/// </summary>
public class Breaker
{
    public Guid Id { get; set; }

    /// <summary>Display name.</summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>Date of birth for age calculation.</summary>
    public DateOnly BirthDate { get; set; }

    /// <summary>UTC timestamp when created.</summary>
    public DateTime CreatedAt { get; set; }

    // Navigation properties
    public ICollection<Registration> Registrations { get; set; } = new List<Registration>();

    /// <summary>
    /// Validates that BirthDate is in the past.
    /// </summary>
    public bool IsValidBirthDate() => BirthDate < DateOnly.FromDateTime(DateTime.UtcNow);

    /// <summary>
    /// Validates that a birth date is in the past (static version).
    /// </summary>
    public static bool IsValidBirthDate(DateOnly birthDate) => birthDate < DateOnly.FromDateTime(DateTime.UtcNow);

    /// <summary>
    /// Calculates the breaker's age at a given event date.
    /// </summary>
    public int GetAgeAtDate(DateOnly eventDate)
    {
        var age = eventDate.Year - BirthDate.Year;
        if (BirthDate > eventDate.AddYears(-age))
        {
            age--;
        }
        return age;
    }

    /// <summary>
    /// Calculates age at a given event date from a birth date (static version).
    /// </summary>
    public static int GetAgeAtDate(DateOnly birthDate, DateOnly eventDate)
    {
        var age = eventDate.Year - birthDate.Year;
        if (birthDate > eventDate.AddYears(-age))
        {
            age--;
        }
        return age;
    }
}
