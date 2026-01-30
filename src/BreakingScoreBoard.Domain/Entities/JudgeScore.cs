namespace BreakingScoreBoard.Domain.Entities;

/// <summary>
/// Individual score submitted by a judge for one breaker in a battle.
/// </summary>
public class JudgeScore
{
    public Guid Id { get; set; }
    
    /// <summary>Parent battle.</summary>
    public Guid BattleId { get; set; }
    
    /// <summary>Which breaker this score is for.</summary>
    public Guid BreakerId { get; set; }
    
    /// <summary>Session/device ID (not a user account).</summary>
    public string JudgeIdentifier { get; set; } = string.Empty;
    
    /// <summary>The score value (0-100).</summary>
    public int Score { get; set; }
    
    /// <summary>UTC timestamp when submitted.</summary>
    public DateTime SubmittedAt { get; set; }
    
    /// <summary>True after reveal countdown starts.</summary>
    public bool IsLocked { get; set; }
    
    // Navigation properties
    public Battle Battle { get; set; } = null!;
    public Breaker Breaker { get; set; } = null!;
    
    /// <summary>
    /// Validates that the score is within the valid range (0-100).
    /// </summary>
    public bool IsValidScore() => Score >= 0 && Score <= 100;
}
