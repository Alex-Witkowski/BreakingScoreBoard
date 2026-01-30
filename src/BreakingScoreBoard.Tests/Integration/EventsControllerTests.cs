using System.Net;
using System.Net.Http.Json;
using BreakingScoreBoard.Api.Contracts.Events;
using BreakingScoreBoard.Api.Infrastructure;
using BreakingScoreBoard.Domain.Entities;
using BreakingScoreBoard.Domain.Enums;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;

namespace BreakingScoreBoard.Tests.Integration;

/// <summary>
/// Integration tests for the EventsController.
/// </summary>
[Collection("Database")]
public class EventsControllerTests : IAsyncLifetime
{
    private readonly IntegrationTestFactory _factory;
    private HttpClient _client = null!;
    
    public EventsControllerTests(IntegrationTestFactory factory)
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
    
    #region T027: POST /events creates event with categories
    
    [Fact]
    public async Task CreateEvent_WithValidRequest_ReturnsCreatedEvent()
    {
        // Arrange
        var request = new CreateEventRequest
        {
            Title = "Test Breaking Championship",
            EventDate = DateOnly.FromDateTime(DateTime.UtcNow.AddMonths(1)),
            Location = "Test Venue",
            JudgeCount = 3,
            AdminPin = "admin123",
            JudgePin = "judge456",
            Categories = new List<CreateCategoryRequest>
            {
                new() { Name = "U14", MaxAge = 14, BracketSize = 16 },
                new() { Name = "Open", MaxAge = null, BracketSize = 32 }
            }
        };
        
        // Act
        var response = await _client.PostAsJsonAsync("/events", request);
        
        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Created);
        
        var eventResponse = await response.Content.ReadFromJsonAsync<EventResponse>();
        eventResponse.Should().NotBeNull();
        eventResponse!.Id.Should().NotBeEmpty();
        eventResponse.Title.Should().Be("Test Breaking Championship");
        eventResponse.JudgeCount.Should().Be(3);
        eventResponse.RegistrationOpen.Should().BeTrue();
        eventResponse.Categories.Should().HaveCount(2);
        eventResponse.Categories[0].Name.Should().Be("U14");
        eventResponse.Categories[0].MaxAge.Should().Be(14);
        eventResponse.Categories[1].Name.Should().Be("Open");
        eventResponse.Categories[1].MaxAge.Should().BeNull();
    }
    
    [Fact]
    public async Task CreateEvent_WithInvalidJudgeCount_ReturnsBadRequest()
    {
        // Arrange
        var request = new CreateEventRequest
        {
            Title = "Test Event",
            EventDate = DateOnly.FromDateTime(DateTime.UtcNow.AddMonths(1)),
            JudgeCount = 4, // Invalid - must be 3 or 5
            AdminPin = "admin123",
            JudgePin = "judge456"
        };
        
        // Act
        var response = await _client.PostAsJsonAsync("/events", request);
        
        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }
    
    [Fact]
    public async Task CreateEvent_WithSamePins_ReturnsBadRequest()
    {
        // Arrange
        var request = new CreateEventRequest
        {
            Title = "Test Event",
            EventDate = DateOnly.FromDateTime(DateTime.UtcNow.AddMonths(1)),
            JudgeCount = 3,
            AdminPin = "same1234",
            JudgePin = "same1234" // Same as admin PIN
        };
        
        // Act
        var response = await _client.PostAsJsonAsync("/events", request);
        
        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }
    
    [Fact]
    public async Task CreateEvent_WithInvalidBracketSize_ReturnsBadRequest()
    {
        // Arrange
        var request = new CreateEventRequest
        {
            Title = "Test Event",
            EventDate = DateOnly.FromDateTime(DateTime.UtcNow.AddMonths(1)),
            JudgeCount = 3,
            AdminPin = "admin123",
            JudgePin = "judge456",
            Categories = new List<CreateCategoryRequest>
            {
                new() { Name = "Invalid", MaxAge = 18, BracketSize = 24 } // Invalid bracket size
            }
        };
        
        // Act
        var response = await _client.PostAsJsonAsync("/events", request);
        
        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }
    
    #endregion
    
    #region T028: GET /events/{id} returns event with categories
    
    [Fact]
    public async Task GetEvent_WithValidId_ReturnsEvent()
    {
        // Arrange - Create an event first
        var createRequest = new CreateEventRequest
        {
            Title = "Get Test Event",
            EventDate = DateOnly.FromDateTime(DateTime.UtcNow.AddMonths(2)),
            Location = "Test Location",
            JudgeCount = 5,
            AdminPin = "admin999",
            JudgePin = "judge888",
            Categories = new List<CreateCategoryRequest>
            {
                new() { Name = "Juniors", MaxAge = 16, BracketSize = 8 }
            }
        };
        
        var createResponse = await _client.PostAsJsonAsync("/events", createRequest);
        var createdEvent = await createResponse.Content.ReadFromJsonAsync<EventResponse>();
        
        // Act
        var response = await _client.GetAsync($"/events/{createdEvent!.Id}");
        
        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        
        var eventResponse = await response.Content.ReadFromJsonAsync<EventResponse>();
        eventResponse.Should().NotBeNull();
        eventResponse!.Id.Should().Be(createdEvent.Id);
        eventResponse.Title.Should().Be("Get Test Event");
        eventResponse.JudgeCount.Should().Be(5);
        eventResponse.Categories.Should().HaveCount(1);
        eventResponse.Categories[0].Name.Should().Be("Juniors");
    }
    
    [Fact]
    public async Task GetEvent_WithInvalidId_ReturnsNotFound()
    {
        // Arrange
        var nonExistentId = Guid.NewGuid();
        
        // Act
        var response = await _client.GetAsync($"/events/{nonExistentId}");
        
        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }
    
    #endregion
    
    #region T029: PATCH /events/{id} blocked after battle starts
    
    [Fact]
    public async Task UpdateEvent_BeforeBattlesStart_Succeeds()
    {
        // Arrange - Create an event
        var createRequest = new CreateEventRequest
        {
            Title = "Update Test Event",
            EventDate = DateOnly.FromDateTime(DateTime.UtcNow.AddMonths(3)),
            JudgeCount = 3,
            AdminPin = "admin111",
            JudgePin = "judge222"
        };
        
        var createResponse = await _client.PostAsJsonAsync("/events", createRequest);
        var createdEvent = await createResponse.Content.ReadFromJsonAsync<EventResponse>();
        
        var updateRequest = new UpdateEventRequest
        {
            Title = "Updated Title",
            RegistrationOpen = false
        };
        
        // Add admin PIN header
        _client.DefaultRequestHeaders.Add("X-Pin", "admin111");
        
        // Act
        var response = await _client.PatchAsJsonAsync($"/events/{createdEvent!.Id}", updateRequest);
        
        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        
        var updatedEvent = await response.Content.ReadFromJsonAsync<EventResponse>();
        updatedEvent.Should().NotBeNull();
        updatedEvent!.Title.Should().Be("Updated Title");
        updatedEvent.RegistrationOpen.Should().BeFalse();
        
        // Cleanup
        _client.DefaultRequestHeaders.Remove("X-Pin");
    }
    
    [Fact]
    public async Task UpdateEvent_AfterBattleStarts_ReturnsBadRequest()
    {
        // Arrange - Create event with category
        var createRequest = new CreateEventRequest
        {
            Title = "Blocked Update Test",
            EventDate = DateOnly.FromDateTime(DateTime.UtcNow.AddMonths(1)),
            JudgeCount = 3,
            AdminPin = "admin333",
            JudgePin = "judge444",
            Categories = new List<CreateCategoryRequest>
            {
                new() { Name = "Test Category", MaxAge = 18, BracketSize = 8 }
            }
        };
        
        var createResponse = await _client.PostAsJsonAsync("/events", createRequest);
        var createdEvent = await createResponse.Content.ReadFromJsonAsync<EventResponse>();
        
        // Create breakers and a battle directly in the database
        using var scope = _factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<BattleDbContext>();
        
        var breaker1 = new Breaker
        {
            Id = Guid.NewGuid(),
            Name = "Breaker 1",
            BirthDate = DateOnly.FromDateTime(DateTime.UtcNow.AddYears(-20)),
            CreatedAt = DateTime.UtcNow
        };
        
        var breaker2 = new Breaker
        {
            Id = Guid.NewGuid(),
            Name = "Breaker 2",
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
            Status = BattleStatus.Scheduled
        };
        
        dbContext.Battles.Add(battle);
        await dbContext.SaveChangesAsync();
        
        var updateRequest = new UpdateEventRequest
        {
            Title = "Should Not Update"
        };
        
        _client.DefaultRequestHeaders.Add("X-Pin", "admin333");
        
        // Act
        var response = await _client.PatchAsJsonAsync($"/events/{createdEvent.Id}", updateRequest);
        
        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        
        // Cleanup
        _client.DefaultRequestHeaders.Remove("X-Pin");
    }
    
    [Fact]
    public async Task UpdateEvent_WithoutAdminPin_ReturnsUnauthorized()
    {
        // Arrange - Create an event
        var createRequest = new CreateEventRequest
        {
            Title = "Auth Test Event",
            EventDate = DateOnly.FromDateTime(DateTime.UtcNow.AddMonths(1)),
            JudgeCount = 3,
            AdminPin = "admin555",
            JudgePin = "judge666"
        };
        
        var createResponse = await _client.PostAsJsonAsync("/events", createRequest);
        var createdEvent = await createResponse.Content.ReadFromJsonAsync<EventResponse>();
        
        var updateRequest = new UpdateEventRequest
        {
            Title = "Should Not Update"
        };
        
        // Act - No PIN header
        var response = await _client.PatchAsJsonAsync($"/events/{createdEvent!.Id}", updateRequest);
        
        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }
    
    #endregion
}
