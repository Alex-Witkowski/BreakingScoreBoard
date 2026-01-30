using BreakingScoreBoard.Domain.Entities;
using FluentAssertions;

namespace BreakingScoreBoard.Tests.Unit.Entities;

/// <summary>
/// Unit tests for Breaker entity.
/// </summary>
public class BreakerTests
{
    #region T059: Age calculation from birth date
    
    [Fact]
    public void GetAgeAtDate_WithBirthdayNotYetPassed_ReturnsCorrectAge()
    {
        // Arrange
        var breaker = new Breaker
        {
            BirthDate = new DateOnly(2005, 6, 15) // Born June 15, 2005
        };
        
        var eventDate = new DateOnly(2026, 3, 1); // Event on March 1, 2026 (before birthday)
        
        // Act
        var age = breaker.GetAgeAtDate(eventDate);
        
        // Assert
        age.Should().Be(20); // Still 20, birthday hasn't occurred yet
    }
    
    [Fact]
    public void GetAgeAtDate_WithBirthdayAlreadyPassed_ReturnsCorrectAge()
    {
        // Arrange
        var breaker = new Breaker
        {
            BirthDate = new DateOnly(2005, 6, 15) // Born June 15, 2005
        };
        
        var eventDate = new DateOnly(2026, 9, 1); // Event on September 1, 2026 (after birthday)
        
        // Act
        var age = breaker.GetAgeAtDate(eventDate);
        
        // Assert
        age.Should().Be(21); // Turned 21 on June 15, 2026
    }
    
    [Fact]
    public void GetAgeAtDate_OnExactBirthday_ReturnsIncrementedAge()
    {
        // Arrange
        var breaker = new Breaker
        {
            BirthDate = new DateOnly(2005, 6, 15)
        };
        
        var eventDate = new DateOnly(2026, 6, 15); // Event on birthday
        
        // Act
        var age = breaker.GetAgeAtDate(eventDate);
        
        // Assert
        age.Should().Be(21);
    }
    
    [Fact]
    public void GetAgeAtDate_YoungBreaker_ReturnsCorrectAge()
    {
        // Arrange
        var breaker = new Breaker
        {
            BirthDate = new DateOnly(2012, 1, 1) // Born January 1, 2012
        };
        
        var eventDate = new DateOnly(2026, 12, 31);
        
        // Act
        var age = breaker.GetAgeAtDate(eventDate);
        
        // Assert
        age.Should().Be(14);
    }
    
    [Fact]
    public void IsValidBirthDate_WithPastDate_ReturnsTrue()
    {
        // Arrange
        var breaker = new Breaker
        {
            BirthDate = DateOnly.FromDateTime(DateTime.UtcNow.AddYears(-20))
        };
        
        // Act
        var result = breaker.IsValidBirthDate();
        
        // Assert
        result.Should().BeTrue();
    }
    
    [Fact]
    public void IsValidBirthDate_WithTodayDate_ReturnsFalse()
    {
        // Arrange
        var breaker = new Breaker
        {
            BirthDate = DateOnly.FromDateTime(DateTime.UtcNow)
        };
        
        // Act
        var result = breaker.IsValidBirthDate();
        
        // Assert
        result.Should().BeFalse();
    }
    
    [Fact]
    public void IsValidBirthDate_WithFutureDate_ReturnsFalse()
    {
        // Arrange
        var breaker = new Breaker
        {
            BirthDate = DateOnly.FromDateTime(DateTime.UtcNow.AddYears(1))
        };
        
        // Act
        var result = breaker.IsValidBirthDate();
        
        // Assert
        result.Should().BeFalse();
    }
    
    #endregion
}
