using BreakingScoreBoard.Domain.Enums;

namespace BreakingScoreBoard.Api.Contracts.Categories;

/// <summary>
/// Response model for an age category.
/// </summary>
public record CategoryResponse
{
    /// <summary>
    /// Unique identifier.
    /// </summary>
    public required Guid Id { get; init; }

    /// <summary>
    /// Parent event ID.
    /// </summary>
    public required Guid EventId { get; init; }

    /// <summary>
    /// Category name (e.g., "U14", "Open").
    /// </summary>
    public required string Name { get; init; }

    /// <summary>
    /// Upper age limit (null = no limit).
    /// </summary>
    public int? MaxAge { get; init; }

    /// <summary>
    /// Target bracket size.
    /// </summary>
    public required int BracketSize { get; init; }

    /// <summary>
    /// Current phase of this category.
    /// </summary>
    public required CategoryPhase CurrentPhase { get; init; }

    /// <summary>
    /// Display order within the event.
    /// </summary>
    public required int SortOrder { get; init; }

    /// <summary>
    /// Number of registered breakers.
    /// </summary>
    public int RegistrationCount { get; init; }
}
