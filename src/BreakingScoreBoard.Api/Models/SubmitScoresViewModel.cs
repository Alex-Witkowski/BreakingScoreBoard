using System.ComponentModel.DataAnnotations;

namespace BreakingScoreBoard.Api.Models;

/// <summary>
/// Mutable view model for submitting scores in the UI.
/// </summary>
public class SubmitScoresViewModel
{
    [Required]
    [Range(1, 3)]
    public int JudgeNumber { get; set; } = 1;

    [Required]
    [Range(0, 100)]
    public float Breaker1Score { get; set; }

    [Required]
    [Range(0, 100)]
    public float Breaker2Score { get; set; }
}
