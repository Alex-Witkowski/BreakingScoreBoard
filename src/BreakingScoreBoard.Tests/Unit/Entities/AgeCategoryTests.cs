using BreakingScoreBoard.Domain.Entities;
using BreakingScoreBoard.Domain.Enums;
using FluentAssertions;

namespace BreakingScoreBoard.Tests.Unit.Entities;

/// <summary>
/// Unit tests for AgeCategory entity validation.
/// </summary>
public class AgeCategoryTests
{
    [Theory]
    [InlineData(null, true)]
    [InlineData(1, true)]
    [InlineData(14, true)]
    [InlineData(18, true)]
    [InlineData(99, true)]
    [InlineData(0, false)]
    [InlineData(-1, false)]
    [InlineData(-10, false)]
    public void IsValidMaxAge_ShouldValidatePositiveOrNull(int? maxAge, bool expected)
    {
        // Arrange
        var category = new AgeCategory { MaxAge = maxAge };
        
        // Act
        var result = category.IsValidMaxAge();
        
        // Assert
        result.Should().Be(expected);
    }
    
    [Theory]
    [InlineData(8, true)]
    [InlineData(16, true)]
    [InlineData(32, true)]
    [InlineData(64, true)]
    [InlineData(4, false)]
    [InlineData(10, false)]
    [InlineData(12, false)]
    [InlineData(24, false)]
    [InlineData(48, false)]
    [InlineData(128, false)]
    [InlineData(0, false)]
    [InlineData(-1, false)]
    public void IsValidBracketSize_ShouldValidatePowerOfTwo(int bracketSize, bool expected)
    {
        // Arrange
        var category = new AgeCategory { BracketSize = bracketSize };
        
        // Act
        var result = category.IsValidBracketSize();
        
        // Assert
        result.Should().Be(expected);
    }
    
    [Fact]
    public void CurrentPhase_ShouldDefaultToRegistration()
    {
        // Arrange & Act
        var category = new AgeCategory();
        
        // Assert
        category.CurrentPhase.Should().Be(CategoryPhase.Registration);
    }
    
    [Fact]
    public void Name_ShouldDefaultToEmptyString()
    {
        // Arrange & Act
        var category = new AgeCategory();
        
        // Assert
        category.Name.Should().BeEmpty();
    }
    
    [Fact]
    public void Registrations_ShouldBeInitializedAsEmptyList()
    {
        // Arrange & Act
        var category = new AgeCategory();
        
        // Assert
        category.Registrations.Should().NotBeNull();
        category.Registrations.Should().BeEmpty();
    }
    
    [Fact]
    public void Battles_ShouldBeInitializedAsEmptyList()
    {
        // Arrange & Act
        var category = new AgeCategory();
        
        // Assert
        category.Battles.Should().NotBeNull();
        category.Battles.Should().BeEmpty();
    }
}
