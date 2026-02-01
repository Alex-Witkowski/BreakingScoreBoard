namespace BreakingScoreBoard.Api.Contracts.Breakers;

/// <summary>
/// Request to update a breaker's information.
/// </summary>
public sealed record UpdateBreakerRequest
{
    /// <summary>
    /// Breaker's legal name.
    /// </summary>
    public required string Name { get; init; }

    /// <summary>
    /// Breaker's battle name (display name for competition).
    /// </summary>
    public required string BattleName { get; init; }

    /// <summary>
    /// Breaker's birth date for age calculation.
    /// </summary>
    public required DateOnly BirthDate { get; init; }
}
