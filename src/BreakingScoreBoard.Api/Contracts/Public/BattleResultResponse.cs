using BreakingScoreBoard.Domain.Enums;

namespace BreakingScoreBoard.Api.Contracts.Public;

/// <summary>
/// Completed battle result.
/// </summary>
public sealed record BattleResultResponse
{
    /// <summary>
    /// Battle ID.
    /// </summary>
    public required Guid BattleId { get; init; }

    /// <summary>
    /// Category name.
    /// </summary>
    public required string CategoryName { get; init; }

    /// <summary>
    /// Bracket level.
    /// </summary>
    public required BracketLevel BracketLevel { get; init; }

    /// <summary>
    /// Winner.
    /// </summary>
    public required BreakerSummary Winner { get; init; }

    /// <summary>
    /// Loser.
    /// </summary>
    public required BreakerSummary Loser { get; init; }

    /// <summary>
    /// Winner's average score (null for walkover).
    /// </summary>
    public decimal? WinnerAvgScore { get; init; }

    /// <summary>
    /// Loser's average score (null for walkover).
    /// </summary>
    public decimal? LoserAvgScore { get; init; }

    /// <summary>
    /// Whether this was a walkover.
    /// </summary>
    public required bool IsWalkover { get; init; }

    /// <summary>
    /// When the battle was completed.
    /// </summary>
    public required DateTime CompletedAt { get; init; }
}
