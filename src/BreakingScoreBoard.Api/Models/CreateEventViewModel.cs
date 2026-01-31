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

    /// <summary>
    /// String representation for text input
    /// </summary>
    public string EventDateString
    {
        get => EventDate.ToString("yyyy-MM-dd");
        set
        {
            if (DateOnly.TryParse(value, out var date))
            {
                EventDate = date;
            }
        }
    }

    /// <summary>
    /// Nullable DateTime for MudBlazor DatePicker compatibility
    /// </summary>
    public DateTime? EventDateNullable 
    { 
        get => EventDate.ToDateTime(TimeOnly.MinValue);
        set => EventDate = value.HasValue ? DateOnly.FromDateTime(value.Value) : DateOnly.FromDateTime(DateTime.Today);
    }

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
