using BreakingScoreBoard.Domain.Enums;

namespace BreakingScoreBoard.Api.Contracts.Registrations;

/// <summary>
/// Response containing registration details.
/// </summary>
public sealed record RegistrationResponse
{
    /// <summary>
    /// Unique registration ID.
    /// </summary>
    public required Guid Id { get; init; }

    /// <summary>
    /// Unique breaker ID.
    /// </summary>
    public required Guid BreakerId { get; init; }

    /// <summary>
    /// Breaker's name.
    /// </summary>
    public required string BreakerName { get; init; }

    /// <summary>
    /// Calculated age at event date.
    /// </summary>
    public required int Age { get; init; }

    /// <summary>
    /// Current registration status.
    /// </summary>
    public required RegistrationStatus Status { get; init; }

    /// <summary>
    /// Timestamp when registration was created or last updated.
    /// </summary>
    public required DateTime RegisteredAt { get; init; }
}
