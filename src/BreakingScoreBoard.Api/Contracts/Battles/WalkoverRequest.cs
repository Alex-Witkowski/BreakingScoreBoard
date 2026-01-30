namespace BreakingScoreBoard.Api.Contracts.Battles;

/// <summary>
/// Request to record a walkover (one breaker no-show).
/// </summary>
public sealed record WalkoverRequest
{
    /// <summary>
    /// ID of the breaker who won by walkover.
    /// </summary>
    public required Guid WinnerId { get; init; }
}
