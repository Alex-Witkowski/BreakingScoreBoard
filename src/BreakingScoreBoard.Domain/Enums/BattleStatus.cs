namespace BreakingScoreBoard.Domain.Enums;

/// <summary>
/// Represents the status of a battle.
/// </summary>
public enum BattleStatus
{
    /// <summary>Battle created, waiting to start.</summary>
    Scheduled,
    
    /// <summary>Judges are scoring.</summary>
    InProgress,
    
    /// <summary>Scores locked, countdown running.</summary>
    RevealCountdown,
    
    /// <summary>Winner determined.</summary>
    Completed,
    
    /// <summary>One breaker no-show, winner set by organizer.</summary>
    Walkover
}
