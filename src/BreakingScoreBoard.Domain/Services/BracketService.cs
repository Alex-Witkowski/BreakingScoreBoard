using BreakingScoreBoard.Domain.Entities;
using BreakingScoreBoard.Domain.Enums;

namespace BreakingScoreBoard.Domain.Services;

/// <summary>
/// Service for handling bracket progression and advancement logic.
/// </summary>
public class BracketService
{
    /// <summary>
    /// Creates the initial bracket from active registrations with random pairing.
    /// Randomly shuffles registrations to prevent registration-time bias for byes and matchups.
    /// </summary>
    /// <param name="category">The category entity.</param>
    /// <param name="activeRegistrations">Active registrations for the category.</param>
    /// <returns>List of created battles.</returns>
    public List<Battle> CreateInitialBracket(AgeCategory category, List<Registration> activeRegistrations)
    {
        if (activeRegistrations.Count < 2)
        {
            return new List<Battle>();
        }

        // Calculate bracket level based on bracket size
        var bracketLevel = category.BracketSize switch
        {
            8 => BracketLevel.Top8,
            16 => BracketLevel.Top16,
            32 => BracketLevel.Top32,
            64 => BracketLevel.Top64,
            _ => BracketLevel.Top16 // Default fallback
        };

        // CRITICAL: Randomly shuffle registrations to prevent registration-time bias
        var random = new Random();
        var shuffledRegistrations = activeRegistrations
            .OrderBy(_ => random.Next())
            .ToList();

        var battles = new List<Battle>();
        var numBattles = activeRegistrations.Count / 2;
        
        // Create battles by pairing consecutive shuffled registrations
        for (int i = 0; i < numBattles; i++)
        {
            var breaker1 = shuffledRegistrations[i * 2];
            var breaker2 = shuffledRegistrations[i * 2 + 1];

            var battle = new Battle
            {
                Id = Guid.NewGuid(),
                CategoryId = category.Id,
                BracketLevel = bracketLevel,
                BracketPosition = i + 1,
                Breaker1Id = breaker1.BreakerId,
                Breaker2Id = breaker2.BreakerId,
                Status = BattleStatus.Scheduled,
                ScheduledAt = DateTime.UtcNow
            };

            battles.Add(battle);
        }

        // Handle bye (green card) if odd number of registrations
        if (activeRegistrations.Count % 2 == 1)
        {
            var byeRegistration = shuffledRegistrations.Last();
            byeRegistration.Status = RegistrationStatus.Advanced;
        }

        return battles;
    }

    /// <summary>
    /// Gets the next bracket level in the progression.
    /// FR-008: Supports automatic advancement through bracket levels.
    /// </summary>
    /// <param name="currentLevel">Current bracket level.</param>
    /// <returns>Next bracket level, or null if already at Final.</returns>
    public static BracketLevel? GetNextBracketLevel(BracketLevel currentLevel)
    {
        return currentLevel switch
        {
            BracketLevel.PreSelection => BracketLevel.Top64,
            BracketLevel.Top64 => BracketLevel.Top32,
            BracketLevel.Top32 => BracketLevel.Top16,
            BracketLevel.Top16 => BracketLevel.Top8,
            BracketLevel.Top8 => BracketLevel.Top4,
            BracketLevel.Top4 => BracketLevel.Final,
            BracketLevel.Final => null,
            _ => null
        };
    }

    /// <summary>
    /// Extracts winners from completed battles.
    /// FR-014: Supports idempotent advancement (same input → same output).
    /// </summary>
    /// <param name="battles">Battles to extract winners from.</param>
    /// <returns>List of winning breakers.</returns>
    public List<Breaker> GetWinnersFromBattles(IEnumerable<Battle> battles)
    {
        return battles
            .Where(b => b.Status == BattleStatus.Completed || b.Status == BattleStatus.Walkover)
            .Where(b => b.Winner != null)
            .Select(b => b.Winner!)
            .ToList();
    }

    /// <summary>
    /// Selects a bye winner when there's an odd number of winners.
    /// FR-009: Random bye selection for odd-number bracket situations.
    /// </summary>
    /// <param name="winners">List of winners.</param>
    /// <returns>Randomly selected bye winner, or null if even count.</returns>
    public Breaker? SelectByeWinner(List<Breaker> winners)
    {
        if (winners.Count % 2 == 0)
        {
            return null; // Even number, no bye needed
        }

        // Random selection
        var random = new Random();
        var index = random.Next(winners.Count);
        return winners[index];
    }

    /// <summary>
    /// Creates battles for the next round from a list of winners.
    /// FR-008: Automatically pairs winners into new battles.
    /// FR-009: Handles odd number of winners with bye selection.
    /// </summary>
    /// <param name="winners">Winners from previous round.</param>
    /// <param name="categoryId">Category ID.</param>
    /// <param name="nextLevel">Next bracket level.</param>
    /// <returns>List of created battles.</returns>
    public List<Battle> CreateNextRoundBattles(
        List<Breaker> winners,
        Guid categoryId,
        BracketLevel nextLevel)
    {
        var battles = new List<Battle>();

        // FR-009: Handle bye if odd number of winners
        var byeWinner = SelectByeWinner(winners);
        var competingWinners = byeWinner != null
            ? winners.Where(w => w.Id != byeWinner.Id).ToList()
            : winners;

        // Pair up winners for battles
        for (int i = 0; i < competingWinners.Count; i += 2)
        {
            if (i + 1 >= competingWinners.Count)
                break; // Safety check (should not happen if bye logic is correct)

            var battle = new Battle
            {
                Id = Guid.NewGuid(),
                CategoryId = categoryId,
                BracketLevel = nextLevel,
                Breaker1Id = competingWinners[i].Id,
                Breaker2Id = competingWinners[i + 1].Id,
                Status = BattleStatus.Scheduled,
                ScheduledAt = DateTime.UtcNow
            };

            battles.Add(battle);
        }

        return battles;
    }

    /// <summary>
    /// Updates registration statuses after advancement.
    /// Winners advance, losers are eliminated.
    /// </summary>
    /// <param name="winners">Winners from battles.</param>
    /// <param name="allRegistrations">All registrations in the category.</param>
    /// <param name="byeWinner">Optional bye winner who advances without a battle.</param>
    public void UpdateRegistrationStatuses(
        List<Breaker> winners,
        List<Registration> allRegistrations,
        Breaker? byeWinner = null)
    {
        var winnerIds = winners.Select(w => w.Id).ToHashSet();
        if (byeWinner != null)
        {
            winnerIds.Add(byeWinner.Id);
        }

        foreach (var registration in allRegistrations)
        {
            if (registration.Status == RegistrationStatus.Active)
            {
                if (winnerIds.Contains(registration.BreakerId))
                {
                    registration.Status = RegistrationStatus.Advanced;
                }
                else
                {
                    registration.Status = RegistrationStatus.Eliminated;
                }
            }
        }
    }
}
