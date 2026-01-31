using BreakingScoreBoard.Domain.Enums;

namespace BreakingScoreBoard.Api.Contracts.Public;

/// <summary>
/// Represents a single matchup in a tournament bracket.
/// </summary>
public sealed record BracketMatchup
{
    /// <summary>
    /// Battle ID.
    /// </summary>
    public required Guid BattleId { get; init; }

    /// <summary>
    /// Bracket level for this matchup.
    /// </summary>
    public required BracketLevel BracketLevel { get; init; }

    /// <summary>
    /// First competitor.
    /// </summary>
    public BreakerSummary? Breaker1 { get; init; }

    /// <summary>
    /// Second competitor.
    /// </summary>
    public BreakerSummary? Breaker2 { get; init; }

    /// <summary>
    /// Winner of this matchup (null if not completed).
    /// </summary>
    public BreakerSummary? Winner { get; init; }

    /// <summary>
    /// Battle status.
    /// </summary>
    public required BattleStatus Status { get; init; }

    /// <summary>
    /// Winner's average score (null for walkover or not completed).
    /// </summary>
    public decimal? WinnerAvgScore { get; init; }

    /// <summary>
    /// Loser's average score (null for walkover or not completed).
    /// </summary>
    public decimal? LoserAvgScore { get; init; }
}
