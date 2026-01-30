using BreakingScoreBoard.Api.Contracts.Scores;
using BreakingScoreBoard.Domain.Enums;

namespace BreakingScoreBoard.Api.Contracts.Battles;

/// <summary>
/// Response model for a battle with detailed scoring information.
/// </summary>
public record BattleDetailResponse
{
    /// <summary>
    /// Unique identifier.
    /// </summary>
    public required Guid Id { get; init; }

    /// <summary>
    /// Category ID.
    /// </summary>
    public required Guid CategoryId { get; init; }

    /// <summary>
    /// Bracket level.
    /// </summary>
    public required BracketLevel BracketLevel { get; init; }

    /// <summary>
    /// Position in bracket (1-based).
    /// </summary>
    public required int BracketPosition { get; init; }

    /// <summary>
    /// First competitor ID.
    /// </summary>
    public required Guid Breaker1Id { get; init; }

    /// <summary>
    /// First competitor name.
    /// </summary>
    public required string Breaker1Name { get; init; }

    /// <summary>
    /// Second competitor ID.
    /// </summary>
    public required Guid Breaker2Id { get; init; }

    /// <summary>
    /// Second competitor name.
    /// </summary>
    public required string Breaker2Name { get; init; }

    /// <summary>
    /// Winner ID (null if not completed).
    /// </summary>
    public Guid? WinnerId { get; init; }

    /// <summary>
    /// Winner name (null if not completed).
    /// </summary>
    public string? WinnerName { get; init; }

    /// <summary>
    /// Battle status.
    /// </summary>
    public required BattleStatus Status { get; init; }

    /// <summary>
    /// Whether this is a re-battle.
    /// </summary>
    public required bool IsReBattle { get; init; }

    /// <summary>
    /// Average score for breaker 1.
    /// </summary>
    public decimal? Breaker1AverageScore { get; init; }

    /// <summary>
    /// Average score for breaker 2.
    /// </summary>
    public decimal? Breaker2AverageScore { get; init; }

    /// <summary>
    /// All judge scores for this battle.
    /// </summary>
    public IList<JudgeScoreResponse> Scores { get; init; } = new List<JudgeScoreResponse>();

    /// <summary>
    /// When battle was completed (if applicable).
    /// </summary>
    public DateTime? CompletedAt { get; init; }
}
