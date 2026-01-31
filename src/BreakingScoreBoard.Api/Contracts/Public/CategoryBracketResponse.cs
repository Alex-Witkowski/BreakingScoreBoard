using BreakingScoreBoard.Domain.Enums;

namespace BreakingScoreBoard.Api.Contracts.Public;

/// <summary>
/// Tournament bracket for a single category.
/// </summary>
public sealed record CategoryBracketResponse
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
    /// Matchups organized by bracket level.
    /// Key is BracketLevel, Value is list of matchups for that level.
    /// </summary>
    public required Dictionary<BracketLevel, List<BracketMatchup>> Rounds { get; init; }

    /// <summary>
    /// Champion (null if finals not completed).
    /// </summary>
    public BreakerSummary? Champion { get; init; }
}
