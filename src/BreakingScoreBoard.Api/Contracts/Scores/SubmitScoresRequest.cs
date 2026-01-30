using System.ComponentModel.DataAnnotations;

namespace BreakingScoreBoard.Api.Contracts.Scores;

/// <summary>
/// Request model for submitting judge scores for a battle.
/// </summary>
public record SubmitScoresRequest
{
    /// <summary>
    /// The battle ID.
    /// </summary>
    [Required]
    public required Guid BattleId { get; init; }

    /// <summary>
    /// Judge identifier (session/device ID).
    /// </summary>
    [Required]
    [StringLength(100, MinimumLength = 1)]
    public required string JudgeIdentifier { get; init; }

    /// <summary>
    /// Score for breaker 1 (0-100).
    /// </summary>
    [Required]
    [Range(0, 100)]
    public required int Breaker1Score { get; init; }

    /// <summary>
    /// Score for breaker 2 (0-100).
    /// </summary>
    [Required]
    [Range(0, 100)]
    public required int Breaker2Score { get; init; }
}
