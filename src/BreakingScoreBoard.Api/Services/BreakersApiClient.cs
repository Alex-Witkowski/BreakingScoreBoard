using BreakingScoreBoard.Api.Contracts.Breakers;

namespace BreakingScoreBoard.Api.Services;

/// <summary>
/// API client for Breakers endpoints
/// </summary>
public class BreakersApiClient : ApiClientBase
{
    public BreakersApiClient(HttpClient httpClient, ILogger<BreakersApiClient> logger)
        : base(httpClient, logger)
    {
    }

    /// <summary>
    /// Get a breaker by ID
    /// </summary>
    public async Task<BreakerResponse> GetBreakerAsync(
        Guid breakerId,
        CancellationToken ct = default)
    {
        return await GetAsync<BreakerResponse>($"/breakers/{breakerId}", ct);
    }

    /// <summary>
    /// Update a breaker's information
    /// </summary>
    public async Task<BreakerResponse> UpdateBreakerAsync(
        Guid breakerId,
        UpdateBreakerRequest request,
        string adminPin,
        CancellationToken ct = default)
    {
        return await PutAsync<UpdateBreakerRequest, BreakerResponse>(
            $"/breakers/{breakerId}",
            request,
            adminPin,
            ct);
    }
}
