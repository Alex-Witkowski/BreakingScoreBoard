namespace BreakingScoreBoard.Api.Contracts.Scores;

/// <summary>
/// Response model for a judge score.
/// </summary>
public record JudgeScoreResponse
{
    /// <summary>
    /// Unique identifier.
    /// </summary>
    public required Guid Id { get; init; }
    
    /// <summary>
    /// Battle ID.
    /// </summary>
    public required Guid BattleId { get; init; }
    
    /// <summary>
    /// Breaker ID this score is for.
    /// </summary>
    public required Guid BreakerId { get; init; }
    
    /// <summary>
    /// Judge identifier.
    /// </summary>
    public required string JudgeIdentifier { get; init; }
    
    /// <summary>
    /// The score value (0-100).
    /// </summary>
    public required int Score { get; init; }
    
    /// <summary>
    /// Whether the score is locked.
    /// </summary>
    public required bool IsLocked { get; init; }
    
    /// <summary>
    /// UTC timestamp when submitted.
    /// </summary>
    public required DateTime SubmittedAt { get; init; }
}
