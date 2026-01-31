using System.ComponentModel.DataAnnotations;

namespace BreakingScoreBoard.Api.Contracts.Events;

/// <summary>
/// Request model for creating a new battle event.
/// </summary>
public record CreateEventRequest
{
    /// <summary>
    /// Event title (e.g., "Berlin Breaking Championship 2026").
    /// </summary>
    [Required]
    [StringLength(200, MinimumLength = 1)]
    public required string Title { get; init; }

    /// <summary>
    /// Date of the competition.
    /// </summary>
    [Required]
    public required DateOnly EventDate { get; init; }

    /// <summary>
    /// Venue name/address (optional).
    /// </summary>
    [StringLength(500)]
    public string? Location { get; init; }

    /// <summary>
    /// Number of judges per battle (3 or 5).
    /// </summary>
    [Required]
    [Range(3, 5)]
    public required int JudgeCount { get; init; }

    /// <summary>
    /// Admin PIN for organizer authentication.
    /// </summary>
    [Required]
    [StringLength(20, MinimumLength = 4)]
    public required string AdminPin { get; init; }

    /// <summary>
    /// Judge PIN for judge authentication.
    /// </summary>
    [Required]
    [StringLength(20, MinimumLength = 4)]
    public required string JudgePin { get; init; }

    /// <summary>
    /// Initial age categories for the event.
    /// </summary>
    public IList<CreateCategoryRequest>? Categories { get; init; }
}

/// <summary>
/// Request model for creating a category within an event.
/// </summary>
public record CreateCategoryRequest
{
    /// <summary>
    /// Category name (e.g., "U14", "Open").
    /// </summary>
    [Required]
    [StringLength(50, MinimumLength = 1)]
    public required string Name { get; init; }

    /// <summary>
    /// Upper age limit (null = no limit, e.g., "Open").
    /// </summary>
    [Range(1, 99)]
    public int? MaxAge { get; init; }

    /// <summary>
    /// Minimum birth year for this category (null = no minimum).
    /// </summary>
    [Range(1900, 2100)]
    public int? MinBirthYear { get; init; }

    /// <summary>
    /// Maximum birth year for this category (null = no maximum).
    /// </summary>
    [Range(1900, 2100)]
    public int? MaxBirthYear { get; init; }

    /// <summary>
    /// Target bracket size (8, 16, 32, or 64).
    /// </summary>
    [Required]
    public required int BracketSize { get; init; }
}
