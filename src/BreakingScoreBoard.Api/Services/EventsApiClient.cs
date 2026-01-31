using BreakingScoreBoard.Api.Contracts.Events;

namespace BreakingScoreBoard.Api.Services;

/// <summary>
/// API client for Events endpoints
/// </summary>
public class EventsApiClient : ApiClientBase
{
    public EventsApiClient(HttpClient httpClient, ILogger<EventsApiClient> logger)
        : base(httpClient, logger)
    {
    }

    /// <summary>
    /// Get all events
    /// </summary>
    public async Task<List<EventResponse>> GetEventsAsync(CancellationToken ct = default)
    {
        return await GetAsync<List<EventResponse>>("/events", ct);
    }

    /// <summary>
    /// Get event by ID
    /// </summary>
    public async Task<EventResponse> GetEventByIdAsync(Guid eventId, CancellationToken ct = default)
    {
        return await GetAsync<EventResponse>($"/events/{eventId}", ct);
    }

    /// <summary>
    /// Create a new event
    /// </summary>
    public async Task<EventResponse> CreateEventAsync(CreateEventRequest request, CancellationToken ct = default)
    {
        return await PostAsync<CreateEventRequest, EventResponse>("/events", request, ct: ct);
    }

    /// <summary>
    /// Update an existing event
    /// </summary>
    public async Task<EventResponse> UpdateEventAsync(
        Guid eventId, 
        UpdateEventRequest request, 
        string adminPin,
        CancellationToken ct = default)
    {
        return await PatchAsync<UpdateEventRequest, EventResponse>(
            $"/events/{eventId}", 
            request, 
            adminPin, 
            ct);
    }

    /// <summary>
    /// Regenerate judge PIN for an event
    /// </summary>
    public async Task<EventResponse> RegenerateJudgePinAsync(
        Guid eventId, 
        string adminPin,
        CancellationToken ct = default)
    {
        return await PostAsync<object, EventResponse>(
            $"/events/{eventId}/regenerate-judge-pin", 
            new { }, 
            adminPin, 
            ct);
    }

    /// <summary>
    /// Delete an event
    /// </summary>
    public async Task DeleteEventAsync(
        Guid eventId,
        string adminPin,
        CancellationToken ct = default)
    {
        await DeleteAsync($"/events/{eventId}", adminPin, ct);
    }
}
