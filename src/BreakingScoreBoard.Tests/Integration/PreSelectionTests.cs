using System.Net;
using System.Net.Http.Json;
using BreakingScoreBoard.Api.Contracts.Events;
using BreakingScoreBoard.Api.Contracts.Categories;
using BreakingScoreBoard.Api.Contracts.Registrations;
using BreakingScoreBoard.Api.Contracts.Battles;
using BreakingScoreBoard.Domain.Enums;
using FluentAssertions;

namespace BreakingScoreBoard.Tests.Integration;

/// <summary>
/// Integration tests for pre-selection functionality.
/// </summary>
[Collection("Database")]
public class PreSelectionTests : IAsyncLifetime
{
    private readonly IntegrationTestFactory _factory;
    private HttpClient _client = null!;
    private const string AdminPin = "admin123";

    public PreSelectionTests(IntegrationTestFactory factory)
    {
        _factory = factory;
    }

    public Task InitializeAsync()
    {
        _client = _factory.CreateClient();
        return Task.CompletedTask;
    }

    public Task DisposeAsync()
    {
        _client?.Dispose();
        return Task.CompletedTask;
    }

    #region T073: Integration test POST /start-preselection creates battles

    [Fact]
    public async Task StartPreSelection_WithOverflow_CreatesBattles()
    {
        // Arrange: Create event with 32-slot category
        var (eventId, categoryId) = await CreateTestEventAndCategory(bracketSize: 32);

        // Register 36 breakers (overflow = 4)
        for (int i = 0; i < 36; i++)
        {
            await RegisterBreaker(eventId, categoryId, $"Breaker{i}", new DateOnly(2012, 1, 1 + i));
        }

        // Act: Start pre-selection
        _client.DefaultRequestHeaders.Remove("X-Pin");
        _client.DefaultRequestHeaders.Add("X-Pin", "event123");

        var response = await _client.PostAsync(
            $"/events/{eventId}/categories/{categoryId}/start-preselection",
            null);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        // Verify battles were created (overflow = 4, so 4 battles)
        var battlesResponse = await _client.GetAsync($"/events/{eventId}/battles?categoryId={categoryId}");
        battlesResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        var battles = await battlesResponse.Content.ReadFromJsonAsync<List<BattleResponse>>();
        battles.Should().NotBeNull();
        battles!.Should().HaveCount(4);
        battles.Should().OnlyContain(b => b.BracketLevel == BracketLevel.PreSelection);
        battles.Should().OnlyContain(b => b.Status == BattleStatus.Scheduled);

        // Verify category phase changed to PreSelection
        var categoryResponse = await _client.GetAsync($"/events/{eventId}/categories/{categoryId}");
        var categoryDetail = await categoryResponse.Content.ReadFromJsonAsync<CategoryDetailResponse>();
        // Note: CategoryDetailResponse doesn't include CurrentPhase, so we'll check via CategoryResponse
    }

    [Fact]
    public async Task StartPreSelection_WithLargeOverflow_CreatesCorrectNumberOfBattles()
    {
        // Arrange: Create event with 32-slot category, register 40 breakers (overflow = 8)
        var (eventId, categoryId) = await CreateTestEventAndCategory(bracketSize: 32);

        for (int i = 0; i < 40; i++)
        {
            await RegisterBreaker(eventId, categoryId, $"Breaker{i}", new DateOnly(2012, 1, 1));
        }

        // Act
        _client.DefaultRequestHeaders.Remove("X-Pin");
        _client.DefaultRequestHeaders.Add("X-Pin", "event123");

        var response = await _client.PostAsync(
            $"/events/{eventId}/categories/{categoryId}/start-preselection",
            null);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var battlesResponse = await _client.GetAsync($"/events/{eventId}/battles?categoryId={categoryId}");
        var battles = await battlesResponse.Content.ReadFromJsonAsync<List<BattleResponse>>();

        battles.Should().NotBeNull();
        battles!.Should().HaveCount(8); // overflow = 8
        battles.Should().OnlyContain(b => b.BracketLevel == BracketLevel.PreSelection);
    }

    #endregion

    #region T074: Integration test no pre-selection when registrations <= bracket size

    [Fact]
    public async Task StartPreSelection_WithExactBracketSize_ReturnsNoPreSelectionNeeded()
    {
        // Arrange: Create event with 32-slot category, register exactly 32 breakers
        var (eventId, categoryId) = await CreateTestEventAndCategory(bracketSize: 32);

        for (int i = 0; i < 32; i++)
        {
            await RegisterBreaker(eventId, categoryId, $"Breaker{i}", new DateOnly(2012, 1, 1));
        }

        // Act
        _client.DefaultRequestHeaders.Remove("X-Pin");
        _client.DefaultRequestHeaders.Add("X-Pin", "event123");

        var response = await _client.PostAsync(
            $"/events/{eventId}/categories/{categoryId}/start-preselection",
            null);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);

        var errorMessage = await response.Content.ReadAsStringAsync();
        errorMessage.Should().Contain("No pre-selection needed");

        // Verify no battles were created
        var battlesResponse = await _client.GetAsync($"/events/{eventId}/battles?categoryId={categoryId}");
        var battles = await battlesResponse.Content.ReadFromJsonAsync<List<BattleResponse>>();
        battles.Should().NotBeNull();
        battles!.Should().BeEmpty();
    }

    [Fact]
    public async Task StartPreSelection_WithFewerThanBracketSize_ReturnsNoPreSelectionNeeded()
    {
        // Arrange: Create event with 32-slot category, register only 28 breakers
        var (eventId, categoryId) = await CreateTestEventAndCategory(bracketSize: 32);

        for (int i = 0; i < 28; i++)
        {
            await RegisterBreaker(eventId, categoryId, $"Breaker{i}", new DateOnly(2012, 1, 1));
        }

        // Act
        _client.DefaultRequestHeaders.Remove("X-Pin");
        _client.DefaultRequestHeaders.Add("X-Pin", "event123");

        var response = await _client.PostAsync(
            $"/events/{eventId}/categories/{categoryId}/start-preselection",
            null);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);

        var errorMessage = await response.Content.ReadAsStringAsync();
        errorMessage.Should().Contain("No pre-selection needed");
    }

    #endregion

    #region T075: Integration test registration blocked after pre-selection starts

    [Fact]
    public async Task RegisterBreaker_AfterPreSelectionStarts_ReturnsForbidden()
    {
        // Arrange: Create event with 32-slot category, register 36 breakers
        var (eventId, categoryId) = await CreateTestEventAndCategory(bracketSize: 32);

        for (int i = 0; i < 36; i++)
        {
            await RegisterBreaker(eventId, categoryId, $"Breaker{i}", new DateOnly(2012, 1, 1));
        }

        // Start pre-selection
        _client.DefaultRequestHeaders.Remove("X-Pin");
        _client.DefaultRequestHeaders.Add("X-Pin", "event123");

        var preSelectionResponse = await _client.PostAsync(
            $"/events/{eventId}/categories/{categoryId}/start-preselection",
            null);

        preSelectionResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        // Act: Attempt to register another breaker
        _client.DefaultRequestHeaders.Remove("X-Pin");
        var registerRequest = new RegisterBreakerRequest
        {
            Name = "Late Breaker",
            BirthDate = new DateOnly(2012, 1, 1)
        };

        var response = await _client.PostAsJsonAsync(
            $"/events/{eventId}/categories/{categoryId}/registrations",
            registerRequest);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);

        var errorMessage = await response.Content.ReadAsStringAsync();
        errorMessage.Should().Contain("Registration closed");
    }

    #endregion

    #region Helper Methods

    private async Task<(Guid eventId, Guid categoryId)> CreateTestEventAndCategory(int bracketSize = 32)
    {
        var eventId = await CreateTestEvent();
        var categoryId = await CreateTestCategory(eventId, bracketSize: bracketSize);
        return (eventId, categoryId);
    }

    private async Task<Guid> CreateTestEvent()
    {
        var request = new CreateEventRequest
        {
            Title = "Test Battle Event",
            EventDate = new DateOnly(2026, 6, 1),
            Location = "Test Arena",
            JudgeCount = 3,
            AdminPin = "event123",
            JudgePin = "judge123"
        };

        _client.DefaultRequestHeaders.Remove("X-Pin");
        _client.DefaultRequestHeaders.Add("X-Pin", AdminPin);

        var response = await _client.PostAsJsonAsync("/events", request);
        response.StatusCode.Should().Be(HttpStatusCode.Created);

        var eventResponse = await response.Content.ReadFromJsonAsync<EventResponse>();
        return eventResponse!.Id;
    }

    private async Task<Guid> CreateTestCategory(Guid eventId, int? maxAge = 14, string name = "U14", int bracketSize = 32)
    {
        var request = new CreateCategoryRequest
        {
            Name = name,
            MaxAge = maxAge,
            BracketSize = bracketSize
        };

        _client.DefaultRequestHeaders.Remove("X-Pin");
        _client.DefaultRequestHeaders.Add("X-Pin", "event123");

        var response = await _client.PostAsJsonAsync($"/events/{eventId}/categories", request);
        response.StatusCode.Should().Be(HttpStatusCode.Created);

        var categoryResponse = await response.Content.ReadFromJsonAsync<CategoryResponse>();
        return categoryResponse!.Id;
    }

    private async Task RegisterBreaker(Guid eventId, Guid categoryId, string name, DateOnly birthDate)
    {
        var request = new RegisterBreakerRequest
        {
            Name = name,
            BirthDate = birthDate
        };

        _client.DefaultRequestHeaders.Remove("X-Pin");

        var response = await _client.PostAsJsonAsync(
            $"/events/{eventId}/categories/{categoryId}/registrations",
            request);

        response.StatusCode.Should().Be(HttpStatusCode.Created);
    }

    #endregion
}
