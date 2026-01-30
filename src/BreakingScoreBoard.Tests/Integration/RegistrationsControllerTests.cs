using System.Net;
using System.Net.Http.Json;
using BreakingScoreBoard.Api.Contracts.Events;
using BreakingScoreBoard.Api.Contracts.Categories;
using BreakingScoreBoard.Api.Contracts.Registrations;
using BreakingScoreBoard.Domain.Enums;
using FluentAssertions;

namespace BreakingScoreBoard.Tests.Integration;

/// <summary>
/// Integration tests for RegistrationsController.
/// </summary>
[Collection("Database")]
public class RegistrationsControllerTests : IAsyncLifetime
{
    private readonly IntegrationTestFactory _factory;
    private HttpClient _client = null!;
    private const string AdminPin = "admin123";

    public RegistrationsControllerTests(IntegrationTestFactory factory)
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

    #region T061: Integration test POST /registrations creates registration

    [Fact]
    public async Task RegisterBreaker_WithValidData_CreatesRegistration()
    {
        // Arrange
        var (eventId, categoryId) = await CreateTestEventAndCategory(
            maxAge: 14,
            categoryName: "U14");

        var request = new RegisterBreakerRequest
        {
            Name = "B-Boy Thunder",
            BirthDate = new DateOnly(2012, 5, 20) // Will be 14 at event date
        };

        // Act
        var response = await _client.PostAsJsonAsync(
            $"/events/{eventId}/categories/{categoryId}/registrations", 
            request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Created);

        var registration = await response.Content.ReadFromJsonAsync<RegistrationResponse>();
        registration.Should().NotBeNull();
        registration!.Id.Should().NotBeEmpty();
        registration.BreakerId.Should().NotBeEmpty();
        registration.BreakerName.Should().Be("B-Boy Thunder");
        registration.Age.Should().Be(14);
        registration.Status.Should().Be(RegistrationStatus.Active);
        registration.RegisteredAt.Should().NotBe(default);

        response.Headers.Location.Should().NotBeNull();
        response.Headers.Location!.ToString().Should()
            .Contain($"/events/{eventId}/categories/{categoryId}/registrations/{registration.BreakerId}");
    }

    [Fact]
    public async Task RegisterBreaker_BreakerExactlyAtMaxAge_Succeeds()
    {
        // Arrange
        var (eventId, categoryId) = await CreateTestEventAndCategory(
            maxAge: 18,
            categoryName: "U18",
            eventDate: new DateOnly(2026, 12, 31));

        var request = new RegisterBreakerRequest
        {
            Name = "B-Girl Storm",
            BirthDate = new DateOnly(2008, 6, 1) // Will be exactly 18 at event
        };

        // Act
        var response = await _client.PostAsJsonAsync(
            $"/events/{eventId}/categories/{categoryId}/registrations", 
            request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Created);

        var registration = await response.Content.ReadFromJsonAsync<RegistrationResponse>();
        registration.Should().NotBeNull();
        registration!.Age.Should().Be(18);
    }

    [Fact]
    public async Task RegisterBreaker_OpenCategory_AcceptsAnyAge()
    {
        // Arrange
        var (eventId, categoryId) = await CreateTestEventAndCategory(
            maxAge: null,
            categoryName: "Open");

        var request = new RegisterBreakerRequest
        {
            Name = "B-Boy OG",
            BirthDate = new DateOnly(1990, 1, 1) // Will be 36 at event
        };

        // Act
        var response = await _client.PostAsJsonAsync(
            $"/events/{eventId}/categories/{categoryId}/registrations", 
            request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Created);

        var registration = await response.Content.ReadFromJsonAsync<RegistrationResponse>();
        registration.Should().NotBeNull();
        registration!.Age.Should().Be(36);
    }

    #endregion

    #region T062: Integration test registration rejected if age doesn't fit category

    [Fact]
    public async Task RegisterBreaker_AgeTooOldForCategory_ReturnsBadRequest()
    {
        // Arrange
        var (eventId, categoryId) = await CreateTestEventAndCategory(
            maxAge: 14,
            categoryName: "U14");

        var request = new RegisterBreakerRequest
        {
            Name = "B-Boy TooOld",
            BirthDate = new DateOnly(2010, 1, 1) // Will be 16 at event
        };

        // Act
        var response = await _client.PostAsJsonAsync(
            $"/events/{eventId}/categories/{categoryId}/registrations", 
            request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);

        var errorMessage = await response.Content.ReadAsStringAsync();
        errorMessage.Should().Contain("age");
        errorMessage.Should().Contain("14");
    }

    [Fact]
    public async Task RegisterBreaker_OneYearTooOld_ReturnsBadRequest()
    {
        // Arrange
        var (eventId, categoryId) = await CreateTestEventAndCategory(
            maxAge: 14,
            categoryName: "U14");

        var request = new RegisterBreakerRequest
        {
            Name = "B-Girl JustMissed",
            BirthDate = new DateOnly(2011, 12, 31) // Will be 15 at event
        };

        // Act
        var response = await _client.PostAsJsonAsync(
            $"/events/{eventId}/categories/{categoryId}/registrations", 
            request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task RegisterBreaker_WithFutureBirthDate_ReturnsBadRequest()
    {
        // Arrange
        var (eventId, categoryId) = await CreateTestEventAndCategory(
            maxAge: 14,
            categoryName: "U14");

        var request = new RegisterBreakerRequest
        {
            Name = "B-Boy Future",
            BirthDate = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(1))
        };

        // Act
        var response = await _client.PostAsJsonAsync(
            $"/events/{eventId}/categories/{categoryId}/registrations", 
            request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);

        var errorMessage = await response.Content.ReadAsStringAsync();
        errorMessage.Should().Contain("birth date");
        errorMessage.Should().Contain("future");
    }

    #endregion

    #region T063: Integration test duplicate registration replaces existing

    [Fact]
    public async Task RegisterBreaker_DuplicateRegistration_ReplacesExisting()
    {
        // Arrange
        var (eventId, categoryId) = await CreateTestEventAndCategory(
            maxAge: 14,
            categoryName: "U14");

        var firstRequest = new RegisterBreakerRequest
        {
            Name = "B-Boy Thunder",
            BirthDate = new DateOnly(2012, 5, 20)
        };

        // Act 1: First registration
        var firstResponse = await _client.PostAsJsonAsync(
            $"/events/{eventId}/categories/{categoryId}/registrations", 
            firstRequest);

        firstResponse.StatusCode.Should().Be(HttpStatusCode.Created);

        var firstRegistration = await firstResponse.Content.ReadFromJsonAsync<RegistrationResponse>();
        var breakerId = firstRegistration!.BreakerId;

        // Act 2: Duplicate registration with same name and birth date
        var secondRequest = new RegisterBreakerRequest
        {
            Name = "B-Boy Thunder",
            BirthDate = new DateOnly(2012, 5, 20)
        };

        var secondResponse = await _client.PostAsJsonAsync(
            $"/events/{eventId}/categories/{categoryId}/registrations", 
            secondRequest);

        // Assert
        secondResponse.StatusCode.Should().Be(HttpStatusCode.Created);

        var secondRegistration = await secondResponse.Content.ReadFromJsonAsync<RegistrationResponse>();
        secondRegistration.Should().NotBeNull();
        secondRegistration!.BreakerId.Should().Be(breakerId); // Same breaker ID
        secondRegistration.BreakerName.Should().Be("B-Boy Thunder");
        
        // Verify only one registration exists in database
        var categoryResponse = await _client.GetAsync($"/events/{eventId}/categories/{categoryId}");
        categoryResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        var categoryDetail = await categoryResponse.Content.ReadFromJsonAsync<CategoryDetailResponse>();
        categoryDetail.Should().NotBeNull();
        categoryDetail!.Registrations.Should().HaveCount(1);
        categoryDetail.Registrations.First().BreakerId.Should().Be(breakerId);
    }

    [Fact]
    public async Task RegisterBreaker_SameBreakerDifferentCategory_CreatesMultipleRegistrations()
    {
        // Arrange
        var eventId = await CreateTestEvent();
        var category1Id = await CreateTestCategory(eventId, maxAge: 14, name: "U14");
        var category2Id = await CreateTestCategory(eventId, maxAge: 18, name: "U18");

        var request = new RegisterBreakerRequest
        {
            Name = "B-Boy Multi",
            BirthDate = new DateOnly(2012, 5, 20) // Age 14, fits both categories
        };

        // Act: Register in first category
        var response1 = await _client.PostAsJsonAsync(
            $"/events/{eventId}/categories/{category1Id}/registrations", 
            request);

        // Act: Register in second category
        var response2 = await _client.PostAsJsonAsync(
            $"/events/{eventId}/categories/{category2Id}/registrations", 
            request);

        // Assert: Both succeed
        response1.StatusCode.Should().Be(HttpStatusCode.Created);
        response2.StatusCode.Should().Be(HttpStatusCode.Created);

        var reg1 = await response1.Content.ReadFromJsonAsync<RegistrationResponse>();
        var reg2 = await response2.Content.ReadFromJsonAsync<RegistrationResponse>();

        // Same breaker, different registrations
        reg1!.BreakerId.Should().Be(reg2!.BreakerId);
        reg1.Id.Should().NotBe(reg2.Id);
    }

    #endregion

    #region Helper Methods

    private async Task<(Guid eventId, Guid categoryId)> CreateTestEventAndCategory(
        int? maxAge = 14, 
        string categoryName = "U14",
        DateOnly? eventDate = null)
    {
        var eventId = await CreateTestEvent(eventDate);
        var categoryId = await CreateTestCategory(eventId, maxAge, categoryName);
        return (eventId, categoryId);
    }

    private async Task<Guid> CreateTestEvent(DateOnly? eventDate = null)
    {
        var request = new CreateEventRequest
        {
            Title = "Test Battle Event",
            EventDate = eventDate ?? new DateOnly(2026, 6, 1),
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

    private async Task<Guid> CreateTestCategory(Guid eventId, int? maxAge, string name)
    {
        var request = new CreateCategoryRequest
        {
            Name = name,
            MaxAge = maxAge,
            BracketSize = 32
        };

        _client.DefaultRequestHeaders.Remove("X-Pin");
        _client.DefaultRequestHeaders.Add("X-Pin", "event123");

        var response = await _client.PostAsJsonAsync($"/events/{eventId}/categories", request);
        response.StatusCode.Should().Be(HttpStatusCode.Created);

        var categoryResponse = await response.Content.ReadFromJsonAsync<CategoryResponse>();
        return categoryResponse!.Id;
    }

    #endregion
}
