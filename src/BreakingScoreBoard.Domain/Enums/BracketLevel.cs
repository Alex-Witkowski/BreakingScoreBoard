namespace BreakingScoreBoard.Domain.Enums;

/// <summary>
/// Represents the bracket level (round) in a tournament.
/// </summary>
public enum BracketLevel
{
    /// <summary>Pre-selection round for overflow participants.</summary>
    PreSelection = 0,

    /// <summary>Round of 64.</summary>
    Top64 = 64,

    /// <summary>Round of 32.</summary>
    Top32 = 32,

    /// <summary>Round of 16.</summary>
    Top16 = 16,

    /// <summary>Quarter-finals (Top 8).</summary>
    Top8 = 8,

    /// <summary>Semi-finals (Top 4).</summary>
    Top4 = 4,

    /// <summary>Finals (Top 2).</summary>
    Final = 2
}
