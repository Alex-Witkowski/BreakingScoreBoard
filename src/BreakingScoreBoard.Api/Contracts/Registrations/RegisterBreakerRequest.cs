namespace BreakingScoreBoard.Api.Contracts.Registrations;

/// <summary>
/// Request to register a breaker for an event category.
/// </summary>
public sealed record RegisterBreakerRequest
{
    /// <summary>
    /// Breaker name (e.g., "B-Boy Thunder").
    /// </summary>
    public required string Name { get; init; }

    /// <summary>
    /// Breaker's birth date for age calculation.
    /// </summary>
    public required DateOnly BirthDate { get; init; }
}
