using BreakingScoreBoard.Domain.Enums;

namespace BreakingScoreBoard.Domain.Entities;

/// <summary>
/// Represents a 1v1 match between two breakers.
/// </summary>
public class Battle
{
    public Guid Id { get; set; }
    
    /// <summary>Parent category.</summary>
    public Guid CategoryId { get; set; }
    
    /// <summary>Bracket level (PreSelection, Top32, etc.).</summary>
    public BracketLevel BracketLevel { get; set; }
    
    /// <summary>Position in bracket (1-based).</summary>
    public int BracketPosition { get; set; }
    
    /// <summary>First competitor.</summary>
    public Guid Breaker1Id { get; set; }
    
    /// <summary>Second competitor.</summary>
    public Guid Breaker2Id { get; set; }
    
    /// <summary>Winner (null until completed).</summary>
    public Guid? WinnerId { get; set; }
    
    /// <summary>Battle status.</summary>
    public BattleStatus Status { get; set; } = BattleStatus.Scheduled;
    
    /// <summary>True if this is a tie re-battle.</summary>
    public bool IsReBattle { get; set; }
    
    /// <summary>Reference to original battle (if re-battle).</summary>
    public Guid? OriginalBattleId { get; set; }
    
    /// <summary>When battle is scheduled to start.</summary>
    public DateTime? ScheduledAt { get; set; }
    
    /// <summary>When battle was completed.</summary>
    public DateTime? CompletedAt { get; set; }
    
    // Navigation properties
    public AgeCategory Category { get; set; } = null!;
    public Breaker Breaker1 { get; set; } = null!;
    public Breaker Breaker2 { get; set; } = null!;
    public Breaker? Winner { get; set; }
    public Battle? OriginalBattle { get; set; }
    public ICollection<JudgeScore> Scores { get; set; } = new List<JudgeScore>();
    
    /// <summary>
    /// Validates that breaker1 and breaker2 are different.
    /// </summary>
    public bool HasDistinctBreakers() => Breaker1Id != Breaker2Id;
    
    /// <summary>
    /// Validates that the winner is one of the competitors.
    /// </summary>
    public bool IsValidWinner() => WinnerId is null || WinnerId == Breaker1Id || WinnerId == Breaker2Id;
}
