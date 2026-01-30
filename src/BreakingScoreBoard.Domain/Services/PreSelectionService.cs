using BreakingScoreBoard.Domain.Entities;
using BreakingScoreBoard.Domain.Enums;

namespace BreakingScoreBoard.Domain.Services;

/// <summary>
/// Service for handling pre-selection logic when registrations exceed bracket capacity.
/// </summary>
public class PreSelectionService
{
    /// <summary>
    /// Calculates the overflow (number of excess registrations beyond bracket capacity).
    /// FR-016: System MUST calculate pre-selection overflow as (registered_count - bracket_size).
    /// </summary>
    /// <param name="registeredCount">Number of active registrations.</param>
    /// <param name="bracketSize">Maximum bracket capacity.</param>
    /// <returns>Overflow count (0 if no overflow).</returns>
    public static int CalculateOverflow(int registeredCount, int bracketSize)
    {
        var overflow = registeredCount - bracketSize;
        return overflow > 0 ? overflow : 0;
    }

    /// <summary>
    /// Randomly selects (2 × overflow) breakers for pre-selection battles.
    /// FR-017: System MUST randomly select (2 × overflow) breakers to compete in pre-selection battles.
    /// </summary>
    /// <param name="registrations">All active registrations.</param>
    /// <param name="overflow">Overflow count.</param>
    /// <returns>Randomly selected registrations for pre-selection.</returns>
    public List<Registration> SelectBreakersForPreSelection(IEnumerable<Registration> registrations, int overflow)
    {
        if (overflow <= 0)
        {
            return new List<Registration>();
        }

        var activeRegistrations = registrations
            .Where(r => r.Status == RegistrationStatus.Active)
            .ToList();

        var selectCount = 2 * overflow;

        // Use Random for shuffling
        var random = new Random();
        var shuffled = activeRegistrations.OrderBy(_ => random.Next()).ToList();

        return shuffled.Take(selectCount).ToList();
    }

    /// <summary>
    /// Creates pre-selection battles from selected breakers.
    /// FR-018: System MUST create (overflow) pre-selection battles.
    /// </summary>
    /// <param name="selectedRegistrations">The (2 × overflow) selected registrations.</param>
    /// <param name="categoryId">Category ID.</param>
    /// <param name="overflow">Overflow count (number of battles to create).</param>
    /// <returns>List of created battles.</returns>
    public List<Battle> CreatePreSelectionBattles(
        List<Registration> selectedRegistrations,
        Guid categoryId,
        int overflow)
    {
        if (overflow <= 0 || selectedRegistrations.Count != 2 * overflow)
        {
            return new List<Battle>();
        }

        var battles = new List<Battle>();

        // Pair up breakers for battles
        for (int i = 0; i < overflow; i++)
        {
            var breaker1 = selectedRegistrations[i * 2];
            var breaker2 = selectedRegistrations[i * 2 + 1];

            var battle = new Battle
            {
                Id = Guid.NewGuid(),
                CategoryId = categoryId,
                BracketLevel = BracketLevel.PreSelection,
                Breaker1Id = breaker1.BreakerId,
                Breaker2Id = breaker2.BreakerId,
                Status = BattleStatus.Scheduled,
                ScheduledAt = DateTime.UtcNow
            };

            battles.Add(battle);
        }

        return battles;
    }
}
