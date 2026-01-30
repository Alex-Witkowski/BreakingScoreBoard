using BreakingScoreBoard.Domain.Enums;

namespace BreakingScoreBoard.Api.Contracts.Public;

/// <summary>
/// Breaker standing in a category.
/// </summary>
public sealed record BreakerStanding
{
    /// <summary>
    /// Breaker ID.
    /// </summary>
    public required Guid BreakerId { get; init; }

    /// <summary>
    /// Breaker name.
    /// </summary>
    public required string BreakerName { get; init; }

    /// <summary>
    /// Current bracket level.
    /// </summary>
    public BracketLevel? CurrentLevel { get; init; }

    /// <summary>
    /// Number of wins.
    /// </summary>
    public required int Wins { get; init; }

    /// <summary>
    /// Number of losses.
    /// </summary>
    public required int Losses { get; init; }

    /// <summary>
    /// Average score across all battles (null if no scored battles).
    /// </summary>
    public decimal? AverageScore { get; init; }

    /// <summary>
    /// Current registration status.
    /// </summary>
    public required RegistrationStatus Status { get; init; }
}
