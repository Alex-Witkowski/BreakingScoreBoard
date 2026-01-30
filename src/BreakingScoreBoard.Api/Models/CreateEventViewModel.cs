using System.ComponentModel.DataAnnotations;

namespace BreakingScoreBoard.Api.Models;

/// <summary>
/// Mutable view model for creating events in the UI.
/// </summary>
public class CreateEventViewModel
{
    [Required]
    [StringLength(200)]
    public string Title { get; set; } = string.Empty;

    [Required]
    public DateOnly EventDate { get; set; } = DateOnly.FromDateTime(DateTime.Today.AddDays(7));

    [StringLength(300)]
    public string? Location { get; set; }

    [Required]
    [Range(3, 5)]
    public int JudgeCount { get; set; } = 3;

    [Required]
    [StringLength(10, MinimumLength = 4)]
    public string AdminPin { get; set; } = string.Empty;

    [Required]
    [StringLength(10, MinimumLength = 4)]
    public string JudgePin { get; set; } = string.Empty;
}
