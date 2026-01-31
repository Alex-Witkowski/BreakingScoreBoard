namespace BreakingScoreBoard.Api.Contracts.Breakers;

/// <summary>
/// Response model for a breaker.
/// </summary>
public sealed record BreakerResponse
{
    /// <summary>
    /// Unique identifier.
    /// </summary>
    public required Guid Id { get; init; }

    /// <summary>
    /// Breaker's legal name.
    /// </summary>
    public required string Name { get; init; }

    /// <summary>
    /// Breaker's battle name (display name for competition).
    /// </summary>
    public required string BattleName { get; init; }

    /// <summary>
    /// Breaker's birth date.
    /// </summary>
    public required DateOnly BirthDate { get; init; }

    /// <summary>
    /// UTC timestamp when created.
    /// </summary>
    public required DateTime CreatedAt { get; init; }
}
