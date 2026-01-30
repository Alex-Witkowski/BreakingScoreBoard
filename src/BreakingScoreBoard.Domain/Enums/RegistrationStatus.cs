namespace BreakingScoreBoard.Domain.Enums;

/// <summary>
/// Represents the status of a breaker's registration in a category.
/// </summary>
public enum RegistrationStatus
{
    /// <summary>In the competition.</summary>
    Active,

    /// <summary>Lost in bracket.</summary>
    Eliminated,

    /// <summary>Won the category (champion).</summary>
    Advanced,

    /// <summary>Removed by organizer.</summary>
    Disqualified
}
