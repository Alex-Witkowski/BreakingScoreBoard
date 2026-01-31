using BreakingScoreBoard.Api.Contracts.Battles;

namespace BreakingScoreBoard.Api.Services;

/// <summary>
/// API client for Battles endpoints
/// </summary>
public class BattlesApiClient : ApiClientBase
{
    public BattlesApiClient(HttpClient httpClient, ILogger<BattlesApiClient> logger)
        : base(httpClient, logger)
    {
    }

    /// <summary>
    /// Get all battles with optional filtering
    /// </summary>
    public async Task<List<BattleResponse>> GetBattlesAsync(
        Guid? eventId = null,
        Guid? categoryId = null,
        string? status = null,
        string? bracketLevel = null,
        CancellationToken ct = default)
    {
        var queryParams = new List<string>();
        if (eventId.HasValue) queryParams.Add($"eventId={eventId.Value}");
        if (categoryId.HasValue) queryParams.Add($"categoryId={categoryId.Value}");
        if (!string.IsNullOrEmpty(status)) queryParams.Add($"status={status}");
        if (!string.IsNullOrEmpty(bracketLevel)) queryParams.Add($"bracketLevel={bracketLevel}");

        var url = "/battles";
        if (queryParams.Any())
        {
            url += "?" + string.Join("&", queryParams);
        }

        return await GetAsync<List<BattleResponse>>(url, ct);
    }

    /// <summary>
    /// Get battle by ID
    /// </summary>
    public async Task<BattleDetailResponse> GetBattleByIdAsync(Guid battleId, CancellationToken ct = default)
    {
        return await GetAsync<BattleDetailResponse>($"/battles/{battleId}", ct);
    }

    /// <summary>
    /// Start a battle
    /// </summary>
    public async Task<BattleDetailResponse> StartBattleAsync(
        Guid battleId,
        string adminPin,
        CancellationToken ct = default)
    {
        return await PostAsync<object, BattleDetailResponse>(
            $"/battles/{battleId}/start",
            new { },
            adminPin,
            ct);
    }

    /// <summary>
    /// Reveal battle scores
    /// </summary>
    public async Task<BattleDetailResponse> RevealBattleAsync(
        Guid battleId,
        string adminPin,
        CancellationToken ct = default)
    {
        return await PostAsync<object, BattleDetailResponse>(
            $"/battles/{battleId}/reveal",
            new { },
            adminPin,
            ct);
    }

    /// <summary>
    /// Mark battle as walkover
    /// </summary>
    public async Task<BattleDetailResponse> WalkoverBattleAsync(
        Guid battleId,
        Guid winnerBreakerId,
        string adminPin,
        CancellationToken ct = default)
    {
        return await PostAsync<object, BattleDetailResponse>(
            $"/battles/{battleId}/walkover",
            new { winnerBreakerId },
            adminPin,
            ct);
    }
}
