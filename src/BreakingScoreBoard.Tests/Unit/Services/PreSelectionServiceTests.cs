using BreakingScoreBoard.Domain.Entities;
using BreakingScoreBoard.Domain.Enums;
using BreakingScoreBoard.Domain.Services;
using FluentAssertions;

namespace BreakingScoreBoard.Tests.Unit.Services;

/// <summary>
/// Unit tests for PreSelectionService.
/// </summary>
public class PreSelectionServiceTests
{
    private readonly PreSelectionService _service = new();

    #region T071: Overflow calculation (registered - bracket size)

    [Fact]
    public void CalculateOverflow_WhenRegistrationsExceedBracketSize_ReturnsCorrectOverflow()
    {
        // Arrange
        var registeredCount = 36;
        var bracketSize = 32;

        // Act
        var overflow = PreSelectionService.CalculateOverflow(registeredCount, bracketSize);

        // Assert
        overflow.Should().Be(4);
    }

    [Fact]
    public void CalculateOverflow_WhenRegistrationsEqualBracketSize_ReturnsZero()
    {
        // Arrange
        var registeredCount = 32;
        var bracketSize = 32;

        // Act
        var overflow = PreSelectionService.CalculateOverflow(registeredCount, bracketSize);

        // Assert
        overflow.Should().Be(0);
    }

    [Fact]
    public void CalculateOverflow_WhenRegistrationsLessThanBracketSize_ReturnsZero()
    {
        // Arrange
        var registeredCount = 28;
        var bracketSize = 32;

        // Act
        var overflow = PreSelectionService.CalculateOverflow(registeredCount, bracketSize);

        // Assert
        overflow.Should().Be(0);
    }

    [Fact]
    public void CalculateOverflow_LargeOverflow_ReturnsCorrectValue()
    {
        // Arrange
        var registeredCount = 40;
        var bracketSize = 32;

        // Act
        var overflow = PreSelectionService.CalculateOverflow(registeredCount, bracketSize);

        // Assert
        overflow.Should().Be(8);
    }

    #endregion

    #region T072: Random selection of 2×overflow breakers

    [Fact]
    public void SelectBreakersForPreSelection_WithOverflow4_Selects8Breakers()
    {
        // Arrange
        var registrations = CreateTestRegistrations(36);
        var overflow = 4;

        // Act
        var selected = _service.SelectBreakersForPreSelection(registrations, overflow);

        // Assert
        selected.Should().HaveCount(8); // 2 × overflow
        selected.Should().OnlyContain(r => registrations.Contains(r));
        selected.Should().OnlyHaveUniqueItems();
    }

    [Fact]
    public void SelectBreakersForPreSelection_WithOverflow8_Selects16Breakers()
    {
        // Arrange
        var registrations = CreateTestRegistrations(40);
        var overflow = 8;

        // Act
        var selected = _service.SelectBreakersForPreSelection(registrations, overflow);

        // Assert
        selected.Should().HaveCount(16); // 2 × overflow
        selected.Should().OnlyContain(r => registrations.Contains(r));
        selected.Should().OnlyHaveUniqueItems();
    }

    [Fact]
    public void SelectBreakersForPreSelection_WithZeroOverflow_ReturnsEmpty()
    {
        // Arrange
        var registrations = CreateTestRegistrations(32);
        var overflow = 0;

        // Act
        var selected = _service.SelectBreakersForPreSelection(registrations, overflow);

        // Assert
        selected.Should().BeEmpty();
    }

    [Fact]
    public void SelectBreakersForPreSelection_IsRandom_DifferentRunsProduceDifferentResults()
    {
        // Arrange
        var registrations = CreateTestRegistrations(36);
        var overflow = 4;

        // Act - Run selection multiple times
        var results = new List<List<Registration>>();
        for (int i = 0; i < 10; i++)
        {
            var service = new PreSelectionService();
            results.Add(service.SelectBreakersForPreSelection(registrations, overflow).ToList());
        }

        // Assert - At least some results should differ (randomness check)
        // We don't require all to be different, but statistically some should be
        var uniqueResults = results.Select(r => string.Join(",", r.Select(x => x.Id).OrderBy(id => id))).Distinct().Count();
        uniqueResults.Should().BeGreaterThan(1, "random selection should produce different results");
    }

    [Fact]
    public void SelectBreakersForPreSelection_OnlySelectsActiveRegistrations()
    {
        // Arrange
        var registrations = CreateTestRegistrations(36);
        registrations[0].Status = RegistrationStatus.Eliminated;
        registrations[1].Status = RegistrationStatus.Disqualified;
        
        var activeCount = registrations.Count(r => r.Status == RegistrationStatus.Active);
        var overflow = 4;

        // Act
        var selected = _service.SelectBreakersForPreSelection(registrations, overflow);

        // Assert
        selected.Should().HaveCount(8);
        selected.Should().OnlyContain(r => r.Status == RegistrationStatus.Active);
    }

    #endregion

    #region Helper Methods

    private static List<Registration> CreateTestRegistrations(int count)
    {
        var registrations = new List<Registration>();
        for (int i = 0; i < count; i++)
        {
            registrations.Add(new Registration
            {
                Id = Guid.NewGuid(),
                BreakerId = Guid.NewGuid(),
                CategoryId = Guid.NewGuid(),
                Status = RegistrationStatus.Active,
                RegisteredAt = DateTime.UtcNow
            });
        }
        return registrations;
    }

    #endregion
}
