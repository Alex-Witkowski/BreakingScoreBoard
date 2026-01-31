using System.Net;
using System.Net.Http.Json;
using BreakingScoreBoard.Api.Contracts.Events;
using BreakingScoreBoard.Api.Contracts.Categories;
using BreakingScoreBoard.Api.Contracts.Registrations;
using BreakingScoreBoard.Api.Contracts.Public;
using BreakingScoreBoard.Api.Contracts.Scores;
using BreakingScoreBoard.Domain.Enums;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using BreakingScoreBoard.Api.Infrastructure;
using Microsoft.EntityFrameworkCore;

namespace BreakingScoreBoard.Tests.Integration;

/// <summary>
/// Integration tests for public (spectator) endpoints.
/// </summary>
[Collection("Database")]
public class PublicControllerTests : IAsyncLifetime
{
    private readonly IntegrationTestFactory _factory;
    private HttpClient _client = null!;
    private const string AdminPin = "admin123";

    public PublicControllerTests(IntegrationTestFactory factory)
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

    #region T094: GET /public/scoreboard returns active battles

    [Fact]
    public async Task GetScoreboard_WithActiveBattles_ReturnsActiveBattlesAndRecentResults()
    {
        // Arrange: Create event with battles
        var (eventId, eventTitle) = await CreateTestEventWithBattles();

        // Act: Get scoreboard (no authentication)
        _client.DefaultRequestHeaders.Remove("X-Pin");
        var response = await _client.GetAsync($"/public/events/{eventId}/scoreboard");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var scoreboard = await response.Content.ReadFromJsonAsync<ScoreboardResponse>();
        scoreboard.Should().NotBeNull();
        scoreboard!.EventId.Should().Be(eventId);
        scoreboard.EventTitle.Should().Be(eventTitle);
        scoreboard.ActiveBattles.Should().NotBeNull();
        scoreboard.RecentResults.Should().NotBeNull();
    }

    [Fact]
    public async Task GetScoreboard_ShowsInProgressBattles()
    {
        // Arrange
        var (eventId, categoryId, battleId) = await CreateBattleAndStartIt();

        // Act
        _client.DefaultRequestHeaders.Remove("X-Pin");
        var response = await _client.GetAsync($"/public/events/{eventId}/scoreboard");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var scoreboard = await response.Content.ReadFromJsonAsync<ScoreboardResponse>();
        scoreboard!.ActiveBattles.Should().ContainSingle(b => b.BattleId == battleId);
        scoreboard.ActiveBattles.First().Status.Should().Be(BattleStatus.InProgress);
    }

    [Fact]
    public async Task GetScoreboard_ShowsRevealCountdownBattles()
    {
        // Arrange
        var (eventId, battleId) = await CreateBattleWithAllScoresSubmitted();

        // Act
        _client.DefaultRequestHeaders.Remove("X-Pin");
        var response = await _client.GetAsync($"/public/events/{eventId}/scoreboard");

        // Assert
        var scoreboard = await response.Content.ReadFromJsonAsync<ScoreboardResponse>();
        scoreboard!.ActiveBattles.Should().ContainSingle(b => b.BattleId == battleId);
        scoreboard.ActiveBattles.First().Status.Should().Be(BattleStatus.RevealCountdown);
        scoreboard.ActiveBattles.First().RevealAt.Should().NotBeNull();
    }

    #endregion

    #region T095: GET /public/standings returns bracket state

    [Fact]
    public async Task GetStandings_ReturnsAllCategoriesWithBreakerStandings()
    {
        // Arrange
        var eventId = await CreateTestEvent();
        var category1Id = await CreateTestCategory(eventId, name: "U14", bracketSize: 32);
        var category2Id = await CreateTestCategory(eventId, name: "U18", bracketSize: 32);

        // Register some breakers
        await RegisterBreaker(eventId, category1Id, "Breaker1", new DateOnly(2012, 1, 1));
        await RegisterBreaker(eventId, category1Id, "Breaker2", new DateOnly(2012, 1, 2));
        await RegisterBreaker(eventId, category2Id, "Breaker3", new DateOnly(2008, 1, 1));

        // Act
        _client.DefaultRequestHeaders.Remove("X-Pin");
        var response = await _client.GetAsync($"/public/events/{eventId}/standings");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var standings = await response.Content.ReadFromJsonAsync<StandingsResponse>();
        standings.Should().NotBeNull();
        standings!.EventId.Should().Be(eventId);
        standings.Categories.Should().HaveCount(2);

        var u14Category = standings.Categories.First(c => c.CategoryName == "U14");
        u14Category.Standings.Should().HaveCount(2);
        u14Category.CurrentPhase.Should().Be(CategoryPhase.Registration);
    }

    [Fact]
    public async Task GetStandings_ShowsWinsAndLosses()
    {
        // Arrange: Create event, battles, and complete them
        var (eventId, categoryId, winnerId, loserId) = await CreateCompletedBattleScenario();

        // Act
        _client.DefaultRequestHeaders.Remove("X-Pin");
        var response = await _client.GetAsync($"/public/events/{eventId}/standings");

        // Assert
        var standings = await response.Content.ReadFromJsonAsync<StandingsResponse>();
        var category = standings!.Categories.First();

        var winner = category.Standings.First(s => s.BreakerId == winnerId);
        winner.Wins.Should().Be(1);
        winner.Losses.Should().Be(0);

        var loser = category.Standings.First(s => s.BreakerId == loserId);
        loser.Wins.Should().Be(0);
        loser.Losses.Should().Be(1);
    }

    #endregion

    #region T096: GET /public/battles/{id}/live returns countdown

    [Fact]
    public async Task GetLiveBattle_WithRevealCountdown_ReturnsCountdownDetails()
    {
        // Arrange
        var (eventId, battleId) = await CreateBattleWithAllScoresSubmitted();

        // Act
        _client.DefaultRequestHeaders.Remove("X-Pin");
        var response = await _client.GetAsync($"/public/events/{eventId}/battles/{battleId}/live");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var liveBattle = await response.Content.ReadFromJsonAsync<LiveBattleResponse>();
        liveBattle.Should().NotBeNull();
        liveBattle!.BattleId.Should().Be(battleId);
        liveBattle.Status.Should().Be(BattleStatus.RevealCountdown);
        liveBattle.JudgesScored.Should().Be(liveBattle.JudgesTotal);
        liveBattle.RevealAt.Should().NotBeNull();
    }

    [Fact]
    public async Task GetLiveBattle_WithInProgress_ShowsJudgeProgress()
    {
        // Arrange
        var (eventId, categoryId, battleId) = await CreateBattleAndStartIt();

        // Submit partial scores (1 of 3 judges)
        _client.DefaultRequestHeaders.Remove("X-Pin");
        _client.DefaultRequestHeaders.Add("X-Pin", "judge123");

        var scoresRequest = new SubmitScoresRequest
        {
            BattleId = battleId,
            JudgeIdentifier = "judge-1",
            Breaker1Score = 85,
            Breaker2Score = 75
        };
        await _client.PostAsJsonAsync($"/events/{eventId}/battles/{battleId}/scores", scoresRequest);

        // Act
        _client.DefaultRequestHeaders.Remove("X-Pin");
        var response = await _client.GetAsync($"/public/events/{eventId}/battles/{battleId}/live");

        // Assert
        var liveBattle = await response.Content.ReadFromJsonAsync<LiveBattleResponse>();
        liveBattle!.JudgesScored.Should().Be(1);
        liveBattle.JudgesTotal.Should().Be(3);
    }

    #endregion

    #region T097: Public endpoints require no authentication

    [Fact]
    public async Task PublicEndpoints_WorkWithoutAuthentication()
    {
        // Arrange
        var eventId = await CreateTestEvent();

        // Act: Call all public endpoints without X-Pin header
        _client.DefaultRequestHeaders.Remove("X-Pin");

        var scoreboardResponse = await _client.GetAsync($"/public/events/{eventId}/scoreboard");
        var standingsResponse = await _client.GetAsync($"/public/events/{eventId}/standings");

        // Assert: All should succeed
        scoreboardResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        standingsResponse.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task PublicEndpoints_IgnoreInvalidPin()
    {
        // Arrange
        var eventId = await CreateTestEvent();

        // Act: Call with invalid PIN
        _client.DefaultRequestHeaders.Remove("X-Pin");
        _client.DefaultRequestHeaders.Add("X-Pin", "invalid-pin");

        var response = await _client.GetAsync($"/public/events/{eventId}/scoreboard");

        // Assert: Still succeeds (PIN ignored for public endpoints)
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    #endregion

    #region T098: GET /public/brackets returns tournament bracket structure

    [Fact]
    public async Task GetBrackets_ReturnsAllCategoriesWithMatchups()
    {
        // Arrange
        var eventId = await CreateTestEvent();
        var categoryId = await CreateTestCategory(eventId, name: "U14", bracketSize: 8);

        // Register breakers and generate brackets
        for (int i = 1; i <= 8; i++)
        {
            await RegisterBreaker(eventId, categoryId, $"Breaker{i}", new DateOnly(2012, 1, i));
        }

        // Generate brackets
        _client.DefaultRequestHeaders.Remove("X-Pin");
        _client.DefaultRequestHeaders.Add("X-Pin", "event123");
        await _client.PostAsync($"/events/{eventId}/categories/{categoryId}/brackets/generate", null);

        // Act: Get brackets (no authentication)
        _client.DefaultRequestHeaders.Remove("X-Pin");
        var response = await _client.GetAsync($"/public/events/{eventId}/brackets");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var brackets = await response.Content.ReadFromJsonAsync<EventBracketsResponse>();
        brackets.Should().NotBeNull();
        brackets!.EventId.Should().Be(eventId);
        brackets.EventTitle.Should().Be("Test Battle Event");
        brackets.Categories.Should().ContainSingle();

        var category = brackets.Categories.First();
        category.CategoryName.Should().Be("U14");
        category.Rounds.Should().NotBeEmpty();
    }

    [Fact]
    public async Task GetBrackets_ShowsCompletedBattleWinners()
    {
        // Arrange: Create completed battle
        var (eventId, categoryId, winnerId, _) = await CreateCompletedBattleScenario();

        // Act
        _client.DefaultRequestHeaders.Remove("X-Pin");
        var response = await _client.GetAsync($"/public/events/{eventId}/brackets");

        // Assert
        var brackets = await response.Content.ReadFromJsonAsync<EventBracketsResponse>();
        var category = brackets!.Categories.First();

        // Find the battle in the rounds
        var allMatchups = category.Rounds.Values.SelectMany(m => m).ToList();
        allMatchups.Should().ContainSingle();

        var matchup = allMatchups.First();
        matchup.Status.Should().Be(BattleStatus.Completed);
        matchup.Winner.Should().NotBeNull();
        matchup.Winner!.Id.Should().Be(winnerId);
    }

    [Fact]
    public async Task GetBrackets_WorksWithMultipleCategories()
    {
        // Arrange
        var eventId = await CreateTestEvent();
        var category1Id = await CreateTestCategory(eventId, name: "U14", bracketSize: 8);
        var category2Id = await CreateTestCategory(eventId, name: "U18", bracketSize: 8);

        // Act
        _client.DefaultRequestHeaders.Remove("X-Pin");
        var response = await _client.GetAsync($"/public/events/{eventId}/brackets");

        // Assert
        var brackets = await response.Content.ReadFromJsonAsync<EventBracketsResponse>();
        brackets!.Categories.Should().HaveCount(2);
        brackets.Categories.Select(c => c.CategoryName).Should().Contain(new[] { "U14", "U18" });
    }

    #endregion

    #region Helper Methods

    private async Task<(Guid eventId, string eventTitle)> CreateTestEventWithBattles()
    {
        var eventId = await CreateTestEvent();
        var eventTitle = "Test Battle Event";
        return (eventId, eventTitle);
    }

    private async Task<(Guid eventId, Guid categoryId, Guid battleId)> CreateBattleAndStartIt()
    {
        var (eventId, categoryId, battleId, _, _) = await CreateTestBattleScenario();

        // Start the battle
        _client.DefaultRequestHeaders.Remove("X-Pin");
        _client.DefaultRequestHeaders.Add("X-Pin", "judge123");
        await _client.PostAsync($"/events/{eventId}/battles/{battleId}/start", null);

        return (eventId, categoryId, battleId);
    }

    private async Task<(Guid eventId, Guid battleId)> CreateBattleWithAllScoresSubmitted()
    {
        var (eventId, categoryId, battleId, _, _) = await CreateTestBattleScenario();

        // Start battle
        _client.DefaultRequestHeaders.Remove("X-Pin");
        _client.DefaultRequestHeaders.Add("X-Pin", "judge123");
        await _client.PostAsync($"/events/{eventId}/battles/{battleId}/start", null);

        // Submit scores from all 3 judges
        for (int i = 1; i <= 3; i++)
        {
            var scoresRequest = new SubmitScoresRequest
            {
                BattleId = battleId,
                JudgeIdentifier = $"judge-{i}",
                Breaker1Score = 85,
                Breaker2Score = 75
            };
            await _client.PostAsJsonAsync($"/events/{eventId}/battles/{battleId}/scores", scoresRequest);
        }

        // Trigger reveal
        _client.DefaultRequestHeaders.Remove("X-Pin");
        _client.DefaultRequestHeaders.Add("X-Pin", "event123");
        await _client.PostAsync($"/events/{eventId}/battles/{battleId}/reveal", null);

        return (eventId, battleId);
    }

    private async Task<(Guid eventId, Guid categoryId, Guid winnerId, Guid loserId)> CreateCompletedBattleScenario()
    {
        var (eventId, categoryId, battleId, breaker1Id, breaker2Id) = await CreateTestBattleScenario();

        // Start battle
        _client.DefaultRequestHeaders.Remove("X-Pin");
        _client.DefaultRequestHeaders.Add("X-Pin", "judge123");
        await _client.PostAsync($"/events/{eventId}/battles/{battleId}/start", null);

        // Submit scores (breaker1 wins)
        for (int i = 1; i <= 3; i++)
        {
            var scoresRequest = new SubmitScoresRequest
            {
                BattleId = battleId,
                JudgeIdentifier = $"judge-{i}",
                Breaker1Score = 85,
                Breaker2Score = 75
            };
            await _client.PostAsJsonAsync($"/events/{eventId}/battles/{battleId}/scores", scoresRequest);
        }

        // Mark as completed by updating in database
        using var scope = _factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<BattleDbContext>();
        var battle = await dbContext.Battles.FindAsync(battleId);
        battle!.Status = BattleStatus.Completed;
        battle.WinnerId = breaker1Id;
        battle.CompletedAt = DateTime.UtcNow;
        await dbContext.SaveChangesAsync();

        return (eventId, categoryId, breaker1Id, breaker2Id);
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
        var eventId = await CreateTestEvent();
        var categoryId = await CreateTestCategory(eventId, bracketSize: 32);

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
