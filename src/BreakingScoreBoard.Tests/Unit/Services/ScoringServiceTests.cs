using BreakingScoreBoard.Domain.Entities;
using BreakingScoreBoard.Domain.Enums;
using BreakingScoreBoard.Domain.Services;
using FluentAssertions;

namespace BreakingScoreBoard.Tests.Unit.Services;

/// <summary>
/// Unit tests for ScoringService.
/// </summary>
public class ScoringServiceTests
{
    private readonly ScoringService _scoringService = new();

    #region T039: Score calculation (average of judge scores)

    [Fact]
    public void CalculateAverageScore_WithMultipleScores_ReturnsAverage()
    {
        // Arrange
        var scores = new List<JudgeScore>
        {
            new() { Score = 80 },
            new() { Score = 85 },
            new() { Score = 90 }
        };

        // Act
        var result = _scoringService.CalculateAverageScore(scores);

        // Assert
        result.Should().Be(85);
    }

    [Fact]
    public void CalculateAverageScore_WithNoScores_ReturnsNull()
    {
        // Arrange
        var scores = new List<JudgeScore>();

        // Act
        var result = _scoringService.CalculateAverageScore(scores);

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public void CalculateAverageScore_WithSingleScore_ReturnsThatScore()
    {
        // Arrange
        var scores = new List<JudgeScore>
        {
            new() { Score = 75 }
        };

        // Act
        var result = _scoringService.CalculateAverageScore(scores);

        // Assert
        result.Should().Be(75);
    }

    [Fact]
    public void DetermineWinner_WithBreaker1HigherAverage_ReturnsBreaker1()
    {
        // Arrange
        var breaker1Id = Guid.NewGuid();
        var breaker2Id = Guid.NewGuid();
        var battleId = Guid.NewGuid();

        var battle = new Battle
        {
            Id = battleId,
            Breaker1Id = breaker1Id,
            Breaker2Id = breaker2Id
        };

        var scores = new List<JudgeScore>
        {
            new() { BattleId = battleId, BreakerId = breaker1Id, Score = 90, JudgeIdentifier = "judge1" },
            new() { BattleId = battleId, BreakerId = breaker1Id, Score = 85, JudgeIdentifier = "judge2" },
            new() { BattleId = battleId, BreakerId = breaker1Id, Score = 88, JudgeIdentifier = "judge3" },
            new() { BattleId = battleId, BreakerId = breaker2Id, Score = 80, JudgeIdentifier = "judge1" },
            new() { BattleId = battleId, BreakerId = breaker2Id, Score = 82, JudgeIdentifier = "judge2" },
            new() { BattleId = battleId, BreakerId = breaker2Id, Score = 81, JudgeIdentifier = "judge3" }
        };

        // Act
        var winner = _scoringService.DetermineWinner(battle, scores);

        // Assert
        winner.Should().Be(breaker1Id);
    }

    [Fact]
    public void DetermineWinner_WithBreaker2HigherAverage_ReturnsBreaker2()
    {
        // Arrange
        var breaker1Id = Guid.NewGuid();
        var breaker2Id = Guid.NewGuid();
        var battleId = Guid.NewGuid();

        var battle = new Battle
        {
            Id = battleId,
            Breaker1Id = breaker1Id,
            Breaker2Id = breaker2Id
        };

        var scores = new List<JudgeScore>
        {
            new() { BattleId = battleId, BreakerId = breaker1Id, Score = 70, JudgeIdentifier = "judge1" },
            new() { BattleId = battleId, BreakerId = breaker1Id, Score = 72, JudgeIdentifier = "judge2" },
            new() { BattleId = battleId, BreakerId = breaker1Id, Score = 71, JudgeIdentifier = "judge3" },
            new() { BattleId = battleId, BreakerId = breaker2Id, Score = 90, JudgeIdentifier = "judge1" },
            new() { BattleId = battleId, BreakerId = breaker2Id, Score = 92, JudgeIdentifier = "judge2" },
            new() { BattleId = battleId, BreakerId = breaker2Id, Score = 91, JudgeIdentifier = "judge3" }
        };

        // Act
        var winner = _scoringService.DetermineWinner(battle, scores);

        // Assert
        winner.Should().Be(breaker2Id);
    }

    [Fact]
    public void DetermineWinner_WithIncompleteScores_ReturnsNull()
    {
        // Arrange
        var breaker1Id = Guid.NewGuid();
        var breaker2Id = Guid.NewGuid();
        var battleId = Guid.NewGuid();

        var battle = new Battle
        {
            Id = battleId,
            Breaker1Id = breaker1Id,
            Breaker2Id = breaker2Id
        };

        // Only breaker1 has scores
        var scores = new List<JudgeScore>
        {
            new() { BattleId = battleId, BreakerId = breaker1Id, Score = 90, JudgeIdentifier = "judge1" }
        };

        // Act
        var winner = _scoringService.DetermineWinner(battle, scores);

        // Assert
        winner.Should().BeNull();
    }

    #endregion

    #region T040: Tie detection triggers re-battle

    [Fact]
    public void IsTie_WithEqualAverageScores_ReturnsTrue()
    {
        // Arrange
        var breaker1Id = Guid.NewGuid();
        var breaker2Id = Guid.NewGuid();
        var battleId = Guid.NewGuid();

        var battle = new Battle
        {
            Id = battleId,
            Breaker1Id = breaker1Id,
            Breaker2Id = breaker2Id
        };

        var scores = new List<JudgeScore>
        {
            new() { BattleId = battleId, BreakerId = breaker1Id, Score = 80, JudgeIdentifier = "judge1" },
            new() { BattleId = battleId, BreakerId = breaker1Id, Score = 85, JudgeIdentifier = "judge2" },
            new() { BattleId = battleId, BreakerId = breaker1Id, Score = 90, JudgeIdentifier = "judge3" },
            new() { BattleId = battleId, BreakerId = breaker2Id, Score = 85, JudgeIdentifier = "judge1" },
            new() { BattleId = battleId, BreakerId = breaker2Id, Score = 85, JudgeIdentifier = "judge2" },
            new() { BattleId = battleId, BreakerId = breaker2Id, Score = 85, JudgeIdentifier = "judge3" }
        };
        // Both average to 85

        // Act
        var result = _scoringService.IsTie(battle, scores);

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public void IsTie_WithDifferentAverageScores_ReturnsFalse()
    {
        // Arrange
        var breaker1Id = Guid.NewGuid();
        var breaker2Id = Guid.NewGuid();
        var battleId = Guid.NewGuid();

        var battle = new Battle
        {
            Id = battleId,
            Breaker1Id = breaker1Id,
            Breaker2Id = breaker2Id
        };

        var scores = new List<JudgeScore>
        {
            new() { BattleId = battleId, BreakerId = breaker1Id, Score = 90, JudgeIdentifier = "judge1" },
            new() { BattleId = battleId, BreakerId = breaker2Id, Score = 85, JudgeIdentifier = "judge1" }
        };

        // Act
        var result = _scoringService.IsTie(battle, scores);

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public void DetermineWinner_WithTie_ReturnsNull()
    {
        // Arrange
        var breaker1Id = Guid.NewGuid();
        var breaker2Id = Guid.NewGuid();
        var battleId = Guid.NewGuid();

        var battle = new Battle
        {
            Id = battleId,
            Breaker1Id = breaker1Id,
            Breaker2Id = breaker2Id
        };

        var scores = new List<JudgeScore>
        {
            new() { BattleId = battleId, BreakerId = breaker1Id, Score = 85, JudgeIdentifier = "judge1" },
            new() { BattleId = battleId, BreakerId = breaker1Id, Score = 85, JudgeIdentifier = "judge2" },
            new() { BattleId = battleId, BreakerId = breaker1Id, Score = 85, JudgeIdentifier = "judge3" },
            new() { BattleId = battleId, BreakerId = breaker2Id, Score = 80, JudgeIdentifier = "judge1" },
            new() { BattleId = battleId, BreakerId = breaker2Id, Score = 85, JudgeIdentifier = "judge2" },
            new() { BattleId = battleId, BreakerId = breaker2Id, Score = 90, JudgeIdentifier = "judge3" }
        };
        // Both average to 85

        // Act
        var winner = _scoringService.DetermineWinner(battle, scores);

        // Assert
        winner.Should().BeNull();
    }

    #endregion

    #region T041: Forced differentiation in re-battles

    [Fact]
    public void ValidateForcedDifferentiation_WhenAllJudgesDifferentiate_ReturnsTrue()
    {
        // Arrange
        var breaker1Id = Guid.NewGuid();
        var breaker2Id = Guid.NewGuid();
        var battleId = Guid.NewGuid();

        var scores = new List<JudgeScore>
        {
            new() { BattleId = battleId, BreakerId = breaker1Id, Score = 90, JudgeIdentifier = "judge1" },
            new() { BattleId = battleId, BreakerId = breaker2Id, Score = 85, JudgeIdentifier = "judge1" },
            new() { BattleId = battleId, BreakerId = breaker1Id, Score = 80, JudgeIdentifier = "judge2" },
            new() { BattleId = battleId, BreakerId = breaker2Id, Score = 82, JudgeIdentifier = "judge2" },
            new() { BattleId = battleId, BreakerId = breaker1Id, Score = 88, JudgeIdentifier = "judge3" },
            new() { BattleId = battleId, BreakerId = breaker2Id, Score = 87, JudgeIdentifier = "judge3" }
        };

        // Act
        var result = _scoringService.ValidateForcedDifferentiation(scores, breaker1Id, breaker2Id);

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public void ValidateForcedDifferentiation_WhenOneJudgeGivesSameScore_ReturnsFalse()
    {
        // Arrange
        var breaker1Id = Guid.NewGuid();
        var breaker2Id = Guid.NewGuid();
        var battleId = Guid.NewGuid();

        var scores = new List<JudgeScore>
        {
            new() { BattleId = battleId, BreakerId = breaker1Id, Score = 90, JudgeIdentifier = "judge1" },
            new() { BattleId = battleId, BreakerId = breaker2Id, Score = 85, JudgeIdentifier = "judge1" },
            new() { BattleId = battleId, BreakerId = breaker1Id, Score = 85, JudgeIdentifier = "judge2" }, // Same score
            new() { BattleId = battleId, BreakerId = breaker2Id, Score = 85, JudgeIdentifier = "judge2" }, // Same score
            new() { BattleId = battleId, BreakerId = breaker1Id, Score = 88, JudgeIdentifier = "judge3" },
            new() { BattleId = battleId, BreakerId = breaker2Id, Score = 87, JudgeIdentifier = "judge3" }
        };

        // Act
        var result = _scoringService.ValidateForcedDifferentiation(scores, breaker1Id, breaker2Id);

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public void ValidateForcedDifferentiation_WithPartialScores_ReturnsTrue()
    {
        // Arrange
        var breaker1Id = Guid.NewGuid();
        var breaker2Id = Guid.NewGuid();
        var battleId = Guid.NewGuid();

        // Judge1 has only scored breaker1, not breaker2 yet
        var scores = new List<JudgeScore>
        {
            new() { BattleId = battleId, BreakerId = breaker1Id, Score = 90, JudgeIdentifier = "judge1" }
        };

        // Act
        var result = _scoringService.ValidateForcedDifferentiation(scores, breaker1Id, breaker2Id);

        // Assert
        result.Should().BeTrue(); // Incomplete scores don't violate differentiation
    }

    #endregion
}
