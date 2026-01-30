using BreakingScoreBoard.Api.Contracts.Categories;

namespace BreakingScoreBoard.Api.Contracts.Events;

/// <summary>
/// Response model for a battle event.
/// </summary>
public record EventResponse
{
    /// <summary>
    /// Unique identifier.
    /// </summary>
    public required Guid Id { get; init; }

    /// <summary>
    /// Event title.
    /// </summary>
    public required string Title { get; init; }

    /// <summary>
    /// Date of the competition.
    /// </summary>
    public required DateOnly EventDate { get; init; }

    /// <summary>
    /// Venue name/address.
    /// </summary>
    public string? Location { get; init; }

    /// <summary>
    /// Number of judges per battle (3 or 5).
    /// </summary>
    public required int JudgeCount { get; init; }

    /// <summary>
    /// Whether new registrations are accepted.
    /// </summary>
    public required bool RegistrationOpen { get; init; }

    /// <summary>
    /// UTC timestamp when created.
    /// </summary>
    public required DateTime CreatedAt { get; init; }

    /// <summary>
    /// UTC timestamp when last updated.
    /// </summary>
    public required DateTime UpdatedAt { get; init; }

    /// <summary>
    /// Age categories for this event.
    /// </summary>
    public IList<CategoryResponse> Categories { get; init; } = new List<CategoryResponse>();
}
