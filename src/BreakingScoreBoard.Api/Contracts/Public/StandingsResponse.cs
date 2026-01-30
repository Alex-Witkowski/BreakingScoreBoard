namespace BreakingScoreBoard.Api.Contracts.Public;

/// <summary>
/// Tournament standings by category.
/// </summary>
public sealed record StandingsResponse
{
    /// <summary>
    /// Event ID.
    /// </summary>
    public required Guid EventId { get; init; }

    /// <summary>
    /// Category standings.
    /// </summary>
    public required List<CategoryStandings> Categories { get; init; }
}
