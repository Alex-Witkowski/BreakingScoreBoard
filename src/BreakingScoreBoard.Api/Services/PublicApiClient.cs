using BreakingScoreBoard.Api.Contracts.Public;

namespace BreakingScoreBoard.Api.Services;

/// <summary>
/// API client for Public endpoints (no authentication required)
/// </summary>
public class PublicApiClient : ApiClientBase
{
    public PublicApiClient(HttpClient httpClient, ILogger<PublicApiClient> logger)
        : base(httpClient, logger)
    {
    }

    /// <summary>
    /// Get scoreboard for an event (active battles and recent results)
    /// </summary>
    public async Task<ScoreboardResponse> GetScoreboardAsync(
        Guid eventId,
        bool includeScheduled = false,
        CancellationToken ct = default)
    {
        var url = $"/public/events/{eventId}/scoreboard";
        if (includeScheduled)
        {
            url += "?includeScheduled=true";
        }

        return await GetAsync<ScoreboardResponse>(url, ct);
    }

    /// <summary>
    /// Get standings for an event (bracket progression and rankings)
    /// </summary>
    public async Task<StandingsResponse> GetStandingsAsync(Guid eventId, CancellationToken ct = default)
    {
        return await GetAsync<StandingsResponse>($"/public/events/{eventId}/standings", ct);
    }

    /// <summary>
    /// Get live battle information with countdown timer
    /// </summary>
    public async Task<LiveBattleResponse> GetLiveBattleAsync(Guid battleId, CancellationToken ct = default)
    {
        return await GetAsync<LiveBattleResponse>($"/public/battles/{battleId}/live", ct);
    }
}
