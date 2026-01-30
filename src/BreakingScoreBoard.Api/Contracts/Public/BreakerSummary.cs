namespace BreakingScoreBoard.Api.Contracts.Public;

/// <summary>
/// Summary information about a breaker.
/// </summary>
public sealed record BreakerSummary
{
    /// <summary>
    /// Breaker ID.
    /// </summary>
    public required Guid Id { get; init; }

    /// <summary>
    /// Breaker name.
    /// </summary>
    public required string Name { get; init; }
}
