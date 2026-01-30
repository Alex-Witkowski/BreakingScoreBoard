using BreakingScoreBoard.Domain.Entities;
using FluentAssertions;

namespace BreakingScoreBoard.Tests.Unit.Entities;

/// <summary>
/// Unit tests for BattleEvent entity validation.
/// </summary>
public class BattleEventTests
{
    [Theory]
    [InlineData(3, true)]
    [InlineData(5, true)]
    [InlineData(1, false)]
    [InlineData(2, false)]
    [InlineData(4, false)]
    [InlineData(7, false)]
    [InlineData(0, false)]
    [InlineData(-1, false)]
    public void IsValidJudgeCount_ShouldValidateOnlyThreeOrFive(int judgeCount, bool expected)
    {
        // Arrange
        var battleEvent = new BattleEvent { JudgeCount = judgeCount };

        // Act
        var result = battleEvent.IsValidJudgeCount();

        // Assert
        result.Should().Be(expected);
    }

    [Fact]
    public void HasDistinctPins_WhenPinsAreDifferent_ShouldReturnTrue()
    {
        // Arrange
        var battleEvent = new BattleEvent
        {
            AdminPinHash = "abc123",
            JudgePinHash = "def456"
        };

        // Act
        var result = battleEvent.HasDistinctPins();

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public void HasDistinctPins_WhenPinsAreSame_ShouldReturnFalse()
    {
        // Arrange
        var battleEvent = new BattleEvent
        {
            AdminPinHash = "abc123",
            JudgePinHash = "abc123"
        };

        // Act
        var result = battleEvent.HasDistinctPins();

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public void Title_ShouldDefaultToEmptyString()
    {
        // Arrange & Act
        var battleEvent = new BattleEvent();

        // Assert
        battleEvent.Title.Should().BeEmpty();
    }

    [Fact]
    public void RegistrationOpen_ShouldDefaultToTrue()
    {
        // Arrange & Act
        var battleEvent = new BattleEvent();

        // Assert
        battleEvent.RegistrationOpen.Should().BeTrue();
    }

    [Fact]
    public void Categories_ShouldBeInitializedAsEmptyList()
    {
        // Arrange & Act
        var battleEvent = new BattleEvent();

        // Assert
        battleEvent.Categories.Should().NotBeNull();
        battleEvent.Categories.Should().BeEmpty();
    }
}
