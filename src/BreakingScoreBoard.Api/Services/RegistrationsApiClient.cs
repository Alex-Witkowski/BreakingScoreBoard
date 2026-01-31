using BreakingScoreBoard.Api.Contracts.Registrations;

namespace BreakingScoreBoard.Api.Services;

/// <summary>
/// API client for Registrations endpoints
/// </summary>
public class RegistrationsApiClient : ApiClientBase
{
    public RegistrationsApiClient(HttpClient httpClient, ILogger<RegistrationsApiClient> logger)
        : base(httpClient, logger)
    {
    }

    /// <summary>
    /// Register a breaker for a category
    /// </summary>
    public async Task<RegistrationResponse> RegisterBreakerAsync(
        RegisterBreakerRequest request,
        string adminPin,
        CancellationToken ct = default)
    {
        return await PostAsync<RegisterBreakerRequest, RegistrationResponse>(
            "/registrations",
            request,
            adminPin,
            ct);
    }

    /// <summary>
    /// Get all registrations for a specific category
    /// </summary>
    public async Task<List<RegistrationResponse>> GetRegistrationsForCategoryAsync(
        Guid eventId,
        Guid categoryId,
        CancellationToken ct = default)
    {
        return await GetAsync<List<RegistrationResponse>>(
            $"/events/{eventId}/categories/{categoryId}/registrations",
            ct);
    }
}
