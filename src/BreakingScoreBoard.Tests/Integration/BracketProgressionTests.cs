using System.Net;
using System.Net.Http.Json;
using BreakingScoreBoard.Api.Contracts.Events;
using BreakingScoreBoard.Api.Contracts.Categories;
using BreakingScoreBoard.Api.Contracts.Registrations;
using BreakingScoreBoard.Api.Contracts.Battles;
using BreakingScoreBoard.Api.Contracts.Scores;
using BreakingScoreBoard.Domain.Enums;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using BreakingScoreBoard.Api.Infrastructure;
using Microsoft.EntityFrameworkCore;

namespace BreakingScoreBoard.Tests.Integration;

/// <summary>
/// Integration tests for bracket progression functionality.
/// </summary>
[Collection("Database")]
public class BracketProgressionTests : IAsyncLifetime
{
    private readonly IntegrationTestFactory _factory;
    private HttpClient _client = null!;
    private const string AdminPin = "admin123";

    public BracketProgressionTests(IntegrationTestFactory factory)
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

    #region T085: Integration test POST /advance-bracket creates next level battles

    [Fact]
    public async Task AdvanceBracket_After32BattlesCompleted_Creates16Top16Battles()
    {
        // Arrange: Create event, category, register 64 breakers, create Top32 battles
        var (eventId, categoryId) = await CreateTestEventAndCategory(bracketSize: 64);

        // Register 64 breakers
        var breakerIds = new List<Guid>();
        for (int i = 0; i < 64; i++)
        {
            var breakerId = await RegisterBreaker(eventId, categoryId, $"Breaker{i}", new DateOnly(2010, 1, 1));
            breakerIds.Add(breakerId);
        }

        // Manually create 32 Top32 battles in database
        using var scope = _factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<BattleDbContext>();

        var battles = new List<Domain.Entities.Battle>();
        for (int i = 0; i < 32; i++)
        {
            var winnerId = breakerIds[i * 2]; // First of each pair wins
            battles.Add(new Domain.Entities.Battle
            {
                Id = Guid.NewGuid(),
                CategoryId = categoryId,
                BracketLevel = BracketLevel.Top32,
                Breaker1Id = breakerIds[i * 2],
                Breaker2Id = breakerIds[i * 2 + 1],
                WinnerId = winnerId,
                Status = BattleStatus.Completed,
                ScheduledAt = DateTime.UtcNow
            });
        }
        dbContext.Battles.AddRange(battles);
        await dbContext.SaveChangesAsync();

        // Act: Advance bracket
        _client.DefaultRequestHeaders.Remove("X-Pin");
        _client.DefaultRequestHeaders.Add("X-Pin", "event123");

        var response = await _client.PostAsync(
            $"/events/{eventId}/categories/{categoryId}/advance-bracket",
            null);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        // Verify Top16 battles were created
        var battlesResponse = await _client.GetAsync($"/events/{eventId}/battles?categoryId={categoryId}");
        var allBattles = await battlesResponse.Content.ReadFromJsonAsync<List<BattleResponse>>();

        allBattles.Should().NotBeNull();
        var top16Battles = allBattles!.Where(b => b.BracketLevel == BracketLevel.Top16).ToList();
        top16Battles.Should().HaveCount(16); // 32 winners → 16 battles
        top16Battles.Should().OnlyContain(b => b.Status == BattleStatus.Scheduled);
    }

    [Fact]
    public async Task AdvanceBracket_WithNoCompletedBattles_ReturnsBadRequest()
    {
        // Arrange
        var (eventId, categoryId) = await CreateTestEventAndCategory(bracketSize: 32);

        // Act: Try to advance without any battles
        _client.DefaultRequestHeaders.Remove("X-Pin");
        _client.DefaultRequestHeaders.Add("X-Pin", "event123");

        var response = await _client.PostAsync(
            $"/events/{eventId}/categories/{categoryId}/advance-bracket",
            null);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);

        var errorMessage = await response.Content.ReadAsStringAsync();
        errorMessage.Should().Contain("No completed battles");
    }

    #endregion

    #region T086: Integration test walkover handling

    [Fact]
    public async Task RecordWalkover_WithValidBattle_SetsWinnerAndWalkoverStatus()
    {
        // Arrange: Create battle
        var (eventId, categoryId, battleId, breaker1Id, breaker2Id) = await CreateTestBattleScenario();

        // Act: Record walkover (breaker1 wins by walkover)
        _client.DefaultRequestHeaders.Remove("X-Pin");
        _client.DefaultRequestHeaders.Add("X-Pin", "event123");

        var walkoverRequest = new { winnerId = breaker1Id };
        var response = await _client.PostAsJsonAsync(
            $"/events/{eventId}/battles/{battleId}/walkover",
            walkoverRequest);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        // Verify battle status
        var battleResponse = await _client.GetFromJsonAsync<BattleDetailResponse>(
            $"/events/{eventId}/battles/{battleId}");

        battleResponse.Should().NotBeNull();
        battleResponse!.Status.Should().Be(BattleStatus.Walkover);
        battleResponse.WinnerId.Should().Be(breaker1Id);

        // FR-028: Verify no scores exist
        var scoresResponse = await _client.GetAsync($"/events/{eventId}/battles/{battleId}/scores");
        var scores = await scoresResponse.Content.ReadFromJsonAsync<List<JudgeScoreResponse>>();
        scores.Should().NotBeNull();
        scores!.Should().BeEmpty();
    }

    [Fact]
    public async Task RecordWalkover_WithInvalidWinner_ReturnsBadRequest()
    {
        // Arrange
        var (eventId, categoryId, battleId, breaker1Id, breaker2Id) = await CreateTestBattleScenario();
        var invalidWinnerId = Guid.NewGuid();

        // Act: Try to set invalid winner
        _client.DefaultRequestHeaders.Remove("X-Pin");
        _client.DefaultRequestHeaders.Add("X-Pin", "event123");

        var walkoverRequest = new { winnerId = invalidWinnerId };
        var response = await _client.PostAsJsonAsync(
            $"/events/{eventId}/battles/{battleId}/walkover",
            walkoverRequest);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);

        var errorMessage = await response.Content.ReadAsStringAsync();
        errorMessage.Should().Contain("not a participant");
    }

    [Fact]
    public async Task RecordWalkover_AfterScoresSubmitted_ReturnsBadRequest()
    {
        // Arrange: Create battle and submit scores
        var (eventId, categoryId, battleId, breaker1Id, breaker2Id) = await CreateTestBattleScenario();

        // Start battle
        _client.DefaultRequestHeaders.Remove("X-Pin");
        _client.DefaultRequestHeaders.Add("X-Pin", "judge123");
        await _client.PostAsync($"/events/{eventId}/battles/{battleId}/start", null);

        // Submit scores
        var scoresRequest = new SubmitScoresRequest
        {
            BattleId = battleId,
            JudgeIdentifier = "judge-1",
            Breaker1Score = 85,
            Breaker2Score = 75
        };
        await _client.PostAsJsonAsync($"/events/{eventId}/battles/{battleId}/scores", scoresRequest);

        // Act: Try to record walkover after scores submitted
        _client.DefaultRequestHeaders.Remove("X-Pin");
        _client.DefaultRequestHeaders.Add("X-Pin", "event123");

        var walkoverRequest = new { winnerId = breaker1Id };
        var response = await _client.PostAsJsonAsync(
            $"/events/{eventId}/battles/{battleId}/walkover",
            walkoverRequest);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);

        var errorMessage = await response.Content.ReadAsStringAsync();
        errorMessage.Should().Contain("scores already submitted");
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

    private async Task<Guid> RegisterBreaker(Guid eventId, Guid categoryId, string name, DateOnly birthDate)
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

        var registration = await response.Content.ReadFromJsonAsync<RegistrationResponse>();
        return registration!.BreakerId;
    }

    private async Task<(Guid eventId, Guid categoryId, Guid battleId, Guid breaker1Id, Guid breaker2Id)> CreateTestBattleScenario()
    {
        var (eventId, categoryId) = await CreateTestEventAndCategory(bracketSize: 32);

        var breaker1Id = await RegisterBreaker(eventId, categoryId, "Breaker1", new DateOnly(2010, 1, 1));
        var breaker2Id = await RegisterBreaker(eventId, categoryId, "Breaker2", new DateOnly(2010, 1, 2));

        // Create battle directly in database
        using var scope = _factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<BattleDbContext>();

        var battle = new Domain.Entities.Battle
        {
            Id = Guid.NewGuid(),
            CategoryId = categoryId,
            BracketLevel = BracketLevel.Top32,
            Breaker1Id = breaker1Id,
            Breaker2Id = breaker2Id,
            Status = BattleStatus.Scheduled,
            ScheduledAt = DateTime.UtcNow
        };
        dbContext.Battles.Add(battle);
        await dbContext.SaveChangesAsync();

        return (eventId, categoryId, battle.Id, breaker1Id, breaker2Id);
    }

    #endregion
}
