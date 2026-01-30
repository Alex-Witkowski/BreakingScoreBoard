using BreakingScoreBoard.Domain.Enums;

namespace BreakingScoreBoard.Api.Contracts.Public;

/// <summary>
/// Category standings showing all breakers and their status.
/// </summary>
public sealed record CategoryStandings
{
    /// <summary>
    /// Category ID.
    /// </summary>
    public required Guid CategoryId { get; init; }

    /// <summary>
    /// Category name.
    /// </summary>
    public required string CategoryName { get; init; }

    /// <summary>
    /// Current phase of the category.
    /// </summary>
    public required CategoryPhase CurrentPhase { get; init; }

    /// <summary>
    /// Current bracket level (highest level with battles).
    /// </summary>
    public BracketLevel? CurrentLevel { get; init; }

    /// <summary>
    /// Breaker standings.
    /// </summary>
    public required List<BreakerStanding> Standings { get; init; }
}
