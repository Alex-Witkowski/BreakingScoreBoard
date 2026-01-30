using BreakingScoreBoard.Domain.Entities;
using BreakingScoreBoard.Domain.Enums;
using FluentAssertions;

namespace BreakingScoreBoard.Tests.Unit.Entities;

/// <summary>
/// Unit tests for Registration entity.
/// </summary>
public class RegistrationTests
{
    #region T060: Age category validation (age fits maxAge)

    [Fact]
    public void Registration_BreakerAgeFitsCategoryMaxAge_IsValid()
    {
        // Arrange
        var eventDate = new DateOnly(2026, 6, 1);

        var breaker = new Breaker
        {
            BirthDate = new DateOnly(2012, 1, 1) // Will be 14 at event
        };

        var category = new AgeCategory
        {
            Name = "U14",
            MaxAge = 14
        };

        // Act
        var breakerAge = breaker.GetAgeAtDate(eventDate);
        var fitsCategory = category.MaxAge is null || breakerAge <= category.MaxAge;

        // Assert
        breakerAge.Should().Be(14);
        fitsCategory.Should().BeTrue();
    }

    [Fact]
    public void Registration_BreakerAgeExceedsCategoryMaxAge_IsInvalid()
    {
        // Arrange
        var eventDate = new DateOnly(2026, 6, 1);

        var breaker = new Breaker
        {
            BirthDate = new DateOnly(2010, 1, 1) // Will be 16 at event
        };

        var category = new AgeCategory
        {
            Name = "U14",
            MaxAge = 14
        };

        // Act
        var breakerAge = breaker.GetAgeAtDate(eventDate);
        var fitsCategory = category.MaxAge is null || breakerAge <= category.MaxAge;

        // Assert
        breakerAge.Should().Be(16);
        fitsCategory.Should().BeFalse();
    }

    [Fact]
    public void Registration_CategoryWithNoMaxAge_AcceptsAnyAge()
    {
        // Arrange
        var eventDate = new DateOnly(2026, 6, 1);

        var breaker = new Breaker
        {
            BirthDate = new DateOnly(1990, 1, 1) // Will be 36 at event
        };

        var category = new AgeCategory
        {
            Name = "Open",
            MaxAge = null // No age limit
        };

        // Act
        var breakerAge = breaker.GetAgeAtDate(eventDate);
        var fitsCategory = category.MaxAge is null || breakerAge <= category.MaxAge;

        // Assert
        breakerAge.Should().Be(36);
        fitsCategory.Should().BeTrue();
    }

    [Fact]
    public void Registration_BreakerExactlyAtMaxAge_IsValid()
    {
        // Arrange
        var eventDate = new DateOnly(2026, 12, 31);

        var breaker = new Breaker
        {
            BirthDate = new DateOnly(2008, 6, 1) // Will be exactly 18 at event
        };

        var category = new AgeCategory
        {
            Name = "U18",
            MaxAge = 18
        };

        // Act
        var breakerAge = breaker.GetAgeAtDate(eventDate);
        var fitsCategory = category.MaxAge is null || breakerAge <= category.MaxAge;

        // Assert
        breakerAge.Should().Be(18);
        fitsCategory.Should().BeTrue();
    }

    [Fact]
    public void Registration_DefaultStatus_IsActive()
    {
        // Arrange & Act
        var registration = new Registration();

        // Assert
        registration.Status.Should().Be(RegistrationStatus.Active);
    }

    #endregion
}
