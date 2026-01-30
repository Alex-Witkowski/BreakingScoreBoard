namespace BreakingScoreBoard.Domain.Enums;

/// <summary>
/// Represents the current phase of an age category in a battle event.
/// </summary>
public enum CategoryPhase
{
    /// <summary>Accepting breaker registrations.</summary>
    Registration,
    
    /// <summary>Running overflow pre-selection battles.</summary>
    PreSelection,
    
    /// <summary>Main knockout bracket in progress.</summary>
    Bracket,
    
    /// <summary>All battles finished, champion determined.</summary>
    Completed
}
