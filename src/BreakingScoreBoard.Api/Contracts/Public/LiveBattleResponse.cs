using BreakingScoreBoard.Domain.Enums;

namespace BreakingScoreBoard.Api.Contracts.Public;

/// <summary>
/// Live battle currently in progress or awaiting reveal.
/// </summary>
public sealed record LiveBattleResponse
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
    /// First breaker.
    /// </summary>
    public required BreakerSummary Breaker1 { get; init; }

    /// <summary>
    /// Second breaker.
    /// </summary>
    public required BreakerSummary Breaker2 { get; init; }

    /// <summary>
    /// Battle status.
    /// </summary>
    public required BattleStatus Status { get; init; }

    /// <summary>
    /// Number of judges who have scored.
    /// </summary>
    public required int JudgesScored { get; init; }

    /// <summary>
    /// Total number of judges for this event.
    /// </summary>
    public required int JudgesTotal { get; init; }

    /// <summary>
    /// Countdown target time (if in RevealCountdown status).
    /// </summary>
    public DateTime? RevealAt { get; init; }
}
