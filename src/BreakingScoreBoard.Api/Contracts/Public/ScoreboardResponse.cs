namespace BreakingScoreBoard.Api.Contracts.Public;

/// <summary>
/// Live scoreboard showing active battles and recent results.
/// </summary>
public sealed record ScoreboardResponse
{
    /// <summary>
    /// Event ID.
    /// </summary>
    public required Guid EventId { get; init; }

    /// <summary>
    /// Event title.
    /// </summary>
    public required string EventTitle { get; init; }

    /// <summary>
    /// Currently active battles (in progress or reveal countdown).
    /// </summary>
    public required List<LiveBattleResponse> ActiveBattles { get; init; }

    /// <summary>
    /// Recently completed battles.
    /// </summary>
    public required List<BattleResultResponse> RecentResults { get; init; }
}
