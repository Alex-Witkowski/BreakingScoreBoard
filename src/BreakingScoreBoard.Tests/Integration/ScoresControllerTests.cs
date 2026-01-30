using System.Net;
using System.Net.Http.Json;
using BreakingScoreBoard.Api.Contracts.Events;
using BreakingScoreBoard.Api.Contracts.Scores;
using BreakingScoreBoard.Api.Infrastructure;
using BreakingScoreBoard.Domain.Entities;
using BreakingScoreBoard.Domain.Enums;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;

namespace BreakingScoreBoard.Tests.Integration;

/// <summary>
/// Integration tests for the ScoresController.
/// </summary>
[Collection("Database")]
public class ScoresControllerTests : IAsyncLifetime
{
    private readonly IntegrationTestFactory _factory;
    private HttpClient _client = null!;

    public ScoresControllerTests(IntegrationTestFactory factory)
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
        _client.Dispose();
        return Task.CompletedTask;
    }

    private async Task<(Guid EventId, Guid CategoryId, Guid BattleId, Guid Breaker1Id, Guid Breaker2Id, string JudgePin)> CreateTestBattle(int judgeCount = 3)
    {
        // Create event
        var createEventRequest = new CreateEventRequest
        {
            Title = $"Score Test Event {Guid.NewGuid()}",
            EventDate = DateOnly.FromDateTime(DateTime.UtcNow.AddMonths(1)),
            JudgeCount = judgeCount,
            AdminPin = $"admin{Guid.NewGuid()}",
            JudgePin = $"judge{Guid.NewGuid()}",
            Categories = new List<CreateCategoryRequest>
            {
                new() { Name = "Test Category", MaxAge = 18, BracketSize = 8 }
            }
        };

        var createResponse = await _client.PostAsJsonAsync("/events", createEventRequest);
        var createdEvent = await createResponse.Content.ReadFromJsonAsync<EventResponse>();

        // Create breakers and battle directly in the database
        using var scope = _factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<BattleDbContext>();

        var breaker1 = new Breaker
        {
            Id = Guid.NewGuid(),
            Name = "Test Breaker 1",
            BirthDate = DateOnly.FromDateTime(DateTime.UtcNow.AddYears(-20)),
            CreatedAt = DateTime.UtcNow
        };

        var breaker2 = new Breaker
        {
            Id = Guid.NewGuid(),
            Name = "Test Breaker 2",
            BirthDate = DateOnly.FromDateTime(DateTime.UtcNow.AddYears(-21)),
            CreatedAt = DateTime.UtcNow
        };

        dbContext.Breakers.AddRange(breaker1, breaker2);

        var battle = new Battle
        {
            Id = Guid.NewGuid(),
            CategoryId = createdEvent!.Categories[0].Id,
            BracketLevel = BracketLevel.Top8,
            BracketPosition = 1,
            Breaker1Id = breaker1.Id,
            Breaker2Id = breaker2.Id,
            Status = BattleStatus.InProgress
        };

        dbContext.Battles.Add(battle);
        await dbContext.SaveChangesAsync();

        return (createdEvent.Id, createdEvent.Categories[0].Id, battle.Id, breaker1.Id, breaker2.Id, createEventRequest.JudgePin);
    }

    #region T042: POST /scores submits judge scores

    [Fact]
    public async Task SubmitScores_WithValidRequest_CreatesScores()
    {
        // Arrange
        var (eventId, categoryId, battleId, breaker1Id, breaker2Id, judgePin) = await CreateTestBattle();

        var request = new SubmitScoresRequest
        {
            BattleId = battleId,
            JudgeIdentifier = "judge1-session",
            Breaker1Score = 85,
            Breaker2Score = 90
        };

        _client.DefaultRequestHeaders.Add("X-Pin", judgePin);

        // Act
        var response = await _client.PostAsJsonAsync("/scores", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Created);

        // Verify scores were created in database
        using var scope = _factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<BattleDbContext>();

        var scores = dbContext.JudgeScores
            .Where(s => s.BattleId == battleId && s.JudgeIdentifier == "judge1-session")
            .ToList();

        scores.Should().HaveCount(2);
        scores.Should().ContainSingle(s => s.BreakerId == breaker1Id && s.Score == 85);
        scores.Should().ContainSingle(s => s.BreakerId == breaker2Id && s.Score == 90);

        // Cleanup
        _client.DefaultRequestHeaders.Remove("X-Pin");
    }

    [Fact]
    public async Task SubmitScores_WithInvalidScoreRange_ReturnsBadRequest()
    {
        // Arrange
        var (eventId, categoryId, battleId, breaker1Id, breaker2Id, judgePin) = await CreateTestBattle();

        var request = new SubmitScoresRequest
        {
            BattleId = battleId,
            JudgeIdentifier = "judge1-session",
            Breaker1Score = 150, // Invalid - out of range
            Breaker2Score = 90
        };

        _client.DefaultRequestHeaders.Add("X-Pin", judgePin);

        // Act
        var response = await _client.PostAsJsonAsync("/scores", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);

        // Cleanup
        _client.DefaultRequestHeaders.Remove("X-Pin");
    }

    #endregion

    #region T043: Score resubmission before reveal allowed

    [Fact]
    public async Task SubmitScores_Resubmission_UpdatesExistingScores()
    {
        // Arrange
        var (eventId, categoryId, battleId, breaker1Id, breaker2Id, judgePin) = await CreateTestBattle();

        var firstRequest = new SubmitScoresRequest
        {
            BattleId = battleId,
            JudgeIdentifier = "judge1-session",
            Breaker1Score = 80,
            Breaker2Score = 85
        };

        var secondRequest = new SubmitScoresRequest
        {
            BattleId = battleId,
            JudgeIdentifier = "judge1-session",
            Breaker1Score = 90, // Updated score
            Breaker2Score = 95  // Updated score
        };

        _client.DefaultRequestHeaders.Add("X-Pin", judgePin);

        // Act - Submit first scores
        var firstResponse = await _client.PostAsJsonAsync("/scores", firstRequest);
        firstResponse.StatusCode.Should().Be(HttpStatusCode.Created);

        // Act - Resubmit with updated scores
        var secondResponse = await _client.PostAsJsonAsync("/scores", secondRequest);

        // Assert
        secondResponse.StatusCode.Should().Be(HttpStatusCode.Created);

        // Verify latest scores in database
        using var scope = _factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<BattleDbContext>();

        var scores = dbContext.JudgeScores
            .Where(s => s.BattleId == battleId && s.JudgeIdentifier == "judge1-session")
            .ToList();

        scores.Should().HaveCount(2);
        scores.Should().ContainSingle(s => s.BreakerId == breaker1Id && s.Score == 90);
        scores.Should().ContainSingle(s => s.BreakerId == breaker2Id && s.Score == 95);

        // Cleanup
        _client.DefaultRequestHeaders.Remove("X-Pin");
    }

    #endregion

    #region T044: Score locked after reveal countdown

    [Fact]
    public async Task SubmitScores_AfterRevealCountdown_ReturnsForbidden()
    {
        // Arrange
        var (eventId, categoryId, battleId, breaker1Id, breaker2Id, judgePin) = await CreateTestBattle();

        // Submit initial scores
        var initialRequest = new SubmitScoresRequest
        {
            BattleId = battleId,
            JudgeIdentifier = "judge1-session",
            Breaker1Score = 85,
            Breaker2Score = 90
        };

        _client.DefaultRequestHeaders.Add("X-Pin", judgePin);
        await _client.PostAsJsonAsync("/scores", initialRequest);

        // Lock scores by triggering reveal
        using (var scope = _factory.Services.CreateScope())
        {
            var dbContext = scope.ServiceProvider.GetRequiredService<BattleDbContext>();
            var battle = await dbContext.Battles.FindAsync(battleId);
            battle!.Status = BattleStatus.RevealCountdown;

            var scores = dbContext.JudgeScores.Where(s => s.BattleId == battleId).ToList();
            foreach (var score in scores)
            {
                score.IsLocked = true;
            }

            await dbContext.SaveChangesAsync();
        }

        // Try to resubmit after locking
        var lockedRequest = new SubmitScoresRequest
        {
            BattleId = battleId,
            JudgeIdentifier = "judge1-session",
            Breaker1Score = 95,
            Breaker2Score = 100
        };

        // Act
        var response = await _client.PostAsJsonAsync("/scores", lockedRequest);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);

        // Cleanup
        _client.DefaultRequestHeaders.Remove("X-Pin");
    }

    #endregion

    #region T045: Score validation (0-100 range)

    [Theory]
    [InlineData(-1, 50)]
    [InlineData(50, -1)]
    [InlineData(101, 50)]
    [InlineData(50, 101)]
    [InlineData(-10, -20)]
    [InlineData(150, 200)]
    public async Task SubmitScores_WithOutOfRangeValues_ReturnsBadRequest(int breaker1Score, int breaker2Score)
    {
        // Arrange
        var (eventId, categoryId, battleId, breaker1Id, breaker2Id, judgePin) = await CreateTestBattle();

        var request = new SubmitScoresRequest
        {
            BattleId = battleId,
            JudgeIdentifier = "judge-test",
            Breaker1Score = breaker1Score,
            Breaker2Score = breaker2Score
        };

        _client.DefaultRequestHeaders.Add("X-Pin", judgePin);

        // Act
        var response = await _client.PostAsJsonAsync("/scores", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);

        // Cleanup
        _client.DefaultRequestHeaders.Remove("X-Pin");
    }

    [Theory]
    [InlineData(0, 0)]
    [InlineData(0, 100)]
    [InlineData(100, 0)]
    [InlineData(100, 100)]
    [InlineData(50, 50)]
    public async Task SubmitScores_WithValidRangeValues_Succeeds(int breaker1Score, int breaker2Score)
    {
        // Arrange
        var (eventId, categoryId, battleId, breaker1Id, breaker2Id, judgePin) = await CreateTestBattle();

        var request = new SubmitScoresRequest
        {
            BattleId = battleId,
            JudgeIdentifier = "judge-test",
            Breaker1Score = breaker1Score,
            Breaker2Score = breaker2Score
        };

        _client.DefaultRequestHeaders.Add("X-Pin", judgePin);

        // Act
        var response = await _client.PostAsJsonAsync("/scores", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Created);

        // Cleanup
        _client.DefaultRequestHeaders.Remove("X-Pin");
    }

    #endregion
}
