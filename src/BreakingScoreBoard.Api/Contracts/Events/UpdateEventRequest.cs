using System.ComponentModel.DataAnnotations;

namespace BreakingScoreBoard.Api.Contracts.Events;

/// <summary>
/// Request model for updating an existing battle event.
/// </summary>
public record UpdateEventRequest
{
    /// <summary>
    /// Event title (e.g., "Berlin Breaking Championship 2026").
    /// </summary>
    [StringLength(200, MinimumLength = 1)]
    public string? Title { get; init; }

    /// <summary>
    /// Date of the competition.
    /// </summary>
    public DateOnly? EventDate { get; init; }

    /// <summary>
    /// Venue name/address.
    /// </summary>
    [StringLength(500)]
    public string? Location { get; init; }

    /// <summary>
    /// Whether new registrations are accepted.
    /// </summary>
    public bool? RegistrationOpen { get; init; }
}
