using BreakingScoreBoard.Domain.Entities;
using BreakingScoreBoard.Domain.Enums;
using BreakingScoreBoard.Domain.Services;
using FluentAssertions;

namespace BreakingScoreBoard.Tests.Unit.Services;

/// <summary>
/// Unit tests for BracketService.
/// </summary>
public class BracketServiceTests
{
    private readonly BracketService _service = new();

    #region T082: Bracket advancement logic

    [Fact]
    public void GetNextBracketLevel_FromPreSelection_ReturnsTop64()
    {
        // Act
        var nextLevel = BracketService.GetNextBracketLevel(BracketLevel.PreSelection);

        // Assert
        nextLevel.Should().Be(BracketLevel.Top64);
    }

    [Fact]
    public void GetNextBracketLevel_FromTop64_ReturnsTop32()
    {
        // Act
        var nextLevel = BracketService.GetNextBracketLevel(BracketLevel.Top64);

        // Assert
        nextLevel.Should().Be(BracketLevel.Top32);
    }

    [Fact]
    public void GetNextBracketLevel_FromTop32_ReturnsTop16()
    {
        // Act
        var nextLevel = BracketService.GetNextBracketLevel(BracketLevel.Top32);

        // Assert
        nextLevel.Should().Be(BracketLevel.Top16);
    }

    [Fact]
    public void GetNextBracketLevel_FromTop16_ReturnsTop8()
    {
        // Act
        var nextLevel = BracketService.GetNextBracketLevel(BracketLevel.Top16);

        // Assert
        nextLevel.Should().Be(BracketLevel.Top8);
    }

    [Fact]
    public void GetNextBracketLevel_FromTop8_ReturnsTop4()
    {
        // Act
        var nextLevel = BracketService.GetNextBracketLevel(BracketLevel.Top8);

        // Assert
        nextLevel.Should().Be(BracketLevel.Top4);
    }

    [Fact]
    public void GetNextBracketLevel_FromTop4_ReturnsFinal()
    {
        // Act
        var nextLevel = BracketService.GetNextBracketLevel(BracketLevel.Top4);

        // Assert
        nextLevel.Should().Be(BracketLevel.Final);
    }

    [Fact]
    public void GetNextBracketLevel_FromFinal_ReturnsNull()
    {
        // Act
        var nextLevel = BracketService.GetNextBracketLevel(BracketLevel.Final);

        // Assert
        nextLevel.Should().BeNull();
    }

    [Fact]
    public void CreateNextRoundBattles_WithEvenWinners_CreatesPairedBattles()
    {
        // Arrange
        var categoryId = Guid.NewGuid();
        var winners = CreateTestWinners(16); // Even number
        var nextLevel = BracketLevel.Top8;

        // Act
        var battles = _service.CreateNextRoundBattles(winners, categoryId, nextLevel);

        // Assert
        battles.Should().HaveCount(8); // 16 winners → 8 battles
        battles.Should().OnlyContain(b => b.BracketLevel == BracketLevel.Top8);
        battles.Should().OnlyContain(b => b.Status == BattleStatus.Scheduled);
        battles.Should().OnlyContain(b => b.Breaker1Id != Guid.Empty && b.Breaker2Id != Guid.Empty);

        // Verify all winners are assigned to battles
        var allBreakers = battles.SelectMany(b => new[] { b.Breaker1Id, b.Breaker2Id }).ToList();
        allBreakers.Should().HaveCount(16);
        allBreakers.Should().OnlyHaveUniqueItems();
    }

    #endregion

    #region T083: Bye assignment for odd winners

    [Fact]
    public void CreateNextRoundBattles_WithOddWinners_AssignsByeAndCreatesByeRecordBattle()
    {
        // Arrange
        var categoryId = Guid.NewGuid();
        var winners = CreateTestWinners(9); // Odd number (9 winners → need 8 for Top8)
        var nextLevel = BracketLevel.Top8;

        // Act
        var battles = _service.CreateNextRoundBattles(winners, categoryId, nextLevel);

        // Assert
        // 9 winners → 1 bye + 8 paired into 4 battles = 5 total battles
        // But actually, we should have 4 battles (8 breakers) and 1 breaker gets a bye
        // The bye breaker doesn't get a battle at this level
        battles.Should().HaveCount(4); // 8 of 9 winners in 4 battles

        var battleBreakers = battles.SelectMany(b => new[] { b.Breaker1Id, b.Breaker2Id }).ToList();
        battleBreakers.Should().HaveCount(8);
        battleBreakers.Should().OnlyHaveUniqueItems();

        // One winner should not be in any battle (received bye)
        var byeWinner = winners.Select(w => w.Id).Except(battleBreakers).Single();
        byeWinner.Should().NotBeEmpty();
    }

    [Fact]
    public void SelectByeWinner_FromOddWinners_SelectsOneRandomly()
    {
        // Arrange
        var winners = CreateTestWinners(9);

        // Act
        var byeWinner = _service.SelectByeWinner(winners);

        // Assert
        byeWinner.Should().NotBeNull();
        winners.Should().Contain(byeWinner);
    }

    [Fact]
    public void SelectByeWinner_WithEvenWinners_ReturnsNull()
    {
        // Arrange
        var winners = CreateTestWinners(16);

        // Act
        var byeWinner = _service.SelectByeWinner(winners);

        // Assert
        byeWinner.Should().BeNull();
    }

    [Fact]
    public void CreateNextRoundBattles_With17Winners_Creates8Battles()
    {
        // Arrange
        var categoryId = Guid.NewGuid();
        var winners = CreateTestWinners(17); // 17 winners → 1 bye + 16 in 8 battles
        var nextLevel = BracketLevel.Top8;

        // Act
        var battles = _service.CreateNextRoundBattles(winners, categoryId, nextLevel);

        // Assert
        battles.Should().HaveCount(8);
    }

    #endregion

    #region T084: Idempotent advancement

    [Fact]
    public void GetWinnersFromBattles_SameInput_ReturnsSameWinners()
    {
        // Arrange
        var battles = CreateCompletedBattles(16); // 16 battles with winners

        // Act
        var winners1 = _service.GetWinnersFromBattles(battles);
        var winners2 = _service.GetWinnersFromBattles(battles);

        // Assert
        winners1.Should().HaveCount(16);
        winners2.Should().HaveCount(16);
        winners1.Select(w => w.Id).Should().BeEquivalentTo(winners2.Select(w => w.Id));
    }

    [Fact]
    public void CreateNextRoundBattles_CalledTwiceWithSameWinners_ProducesSimilarStructure()
    {
        // Arrange
        var categoryId = Guid.NewGuid();
        var winners = CreateTestWinners(16);
        var nextLevel = BracketLevel.Top8;

        // Act - Call twice
        var battles1 = _service.CreateNextRoundBattles(winners, categoryId, nextLevel);
        var battles2 = _service.CreateNextRoundBattles(winners, categoryId, nextLevel);

        // Assert - Should create same number of battles with same participants
        battles1.Should().HaveCount(8);
        battles2.Should().HaveCount(8);

        var breakers1 = battles1.SelectMany(b => new[] { b.Breaker1Id, b.Breaker2Id }).OrderBy(id => id).ToList();
        var breakers2 = battles2.SelectMany(b => new[] { b.Breaker1Id, b.Breaker2Id }).OrderBy(id => id).ToList();

        breakers1.Should().BeEquivalentTo(breakers2);
    }

    [Fact]
    public void GetWinnersFromBattles_OnlyIncludesCompletedBattles()
    {
        // Arrange
        var completedBattles = CreateCompletedBattles(10);
        var scheduledBattles = CreateScheduledBattles(5);
        var allBattles = completedBattles.Concat(scheduledBattles).ToList();

        // Act
        var winners = _service.GetWinnersFromBattles(allBattles);

        // Assert
        winners.Should().HaveCount(10); // Only from completed battles
    }

    [Fact]
    public void GetWinnersFromBattles_IncludesWalkoverBattles()
    {
        // Arrange
        var completedBattles = CreateCompletedBattles(8);
        var walkoverBattles = CreateWalkoverBattles(2);
        var allBattles = completedBattles.Concat(walkoverBattles).ToList();

        // Act
        var winners = _service.GetWinnersFromBattles(allBattles);

        // Assert
        winners.Should().HaveCount(10); // 8 completed + 2 walkover
    }

    #endregion

    #region Helper Methods

    private static List<Breaker> CreateTestWinners(int count)
    {
        var winners = new List<Breaker>();
        for (int i = 0; i < count; i++)
        {
            winners.Add(new Breaker
            {
                Id = Guid.NewGuid(),
                Name = $"Winner{i + 1}",
                BirthDate = new DateOnly(2010, 1, 1)
            });
        }
        return winners;
    }

    private static List<Battle> CreateCompletedBattles(int count)
    {
        var battles = new List<Battle>();
        for (int i = 0; i < count; i++)
        {
            var winnerId = Guid.NewGuid();
            battles.Add(new Battle
            {
                Id = Guid.NewGuid(),
                CategoryId = Guid.NewGuid(),
                BracketLevel = BracketLevel.Top32,
                Breaker1Id = winnerId,
                Breaker2Id = Guid.NewGuid(),
                WinnerId = winnerId,
                Winner = new Breaker { Id = winnerId, Name = $"Winner{i + 1}", BirthDate = new DateOnly(2010, 1, 1) },
                Status = BattleStatus.Completed
            });
        }
        return battles;
    }

    private static List<Battle> CreateScheduledBattles(int count)
    {
        var battles = new List<Battle>();
        for (int i = 0; i < count; i++)
        {
            battles.Add(new Battle
            {
                Id = Guid.NewGuid(),
                CategoryId = Guid.NewGuid(),
                BracketLevel = BracketLevel.Top32,
                Breaker1Id = Guid.NewGuid(),
                Breaker2Id = Guid.NewGuid(),
                Status = BattleStatus.Scheduled
            });
        }
        return battles;
    }

    private static List<Battle> CreateWalkoverBattles(int count)
    {
        var battles = new List<Battle>();
        for (int i = 0; i < count; i++)
        {
            var winnerId = Guid.NewGuid();
            battles.Add(new Battle
            {
                Id = Guid.NewGuid(),
                CategoryId = Guid.NewGuid(),
                BracketLevel = BracketLevel.Top32,
                Breaker1Id = winnerId,
                Breaker2Id = Guid.NewGuid(),
                WinnerId = winnerId,
                Winner = new Breaker { Id = winnerId, Name = $"WalkoverWinner{i + 1}", BirthDate = new DateOnly(2010, 1, 1) },
                Status = BattleStatus.Walkover
            });
        }
        return battles;
    }

    #endregion
}
