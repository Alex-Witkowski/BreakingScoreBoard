using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using BreakingScoreBoard.Api.Contracts.Public;
using BreakingScoreBoard.Api.Infrastructure;
using BreakingScoreBoard.Domain.Enums;
using BreakingScoreBoard.Domain.Services;

namespace BreakingScoreBoard.Api.Controllers;

/// <summary>
/// Public endpoints for spectators (no authentication required).
/// </summary>
[ApiController]
[Route("public/events/{eventId:guid}")]
[Produces("application/json")]
public class PublicController : ControllerBase
{
    private readonly BattleDbContext _context;
    private readonly ScoringService _scoringService;

    public PublicController(BattleDbContext context, ScoringService scoringService)
    {
        _context = context;
        _scoringService = scoringService;
    }

    /// <summary>
    /// Gets live scoreboard showing active battles and recent results.
    /// </summary>
    /// <param name="eventId">Event ID.</param>
    /// <returns>Scoreboard with active battles and recent results.</returns>
    /// <response code="200">Scoreboard retrieved successfully</response>
    /// <response code="404">Event not found</response>
    [HttpGet("scoreboard")]
    [ProducesResponseType(typeof(ScoreboardResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetScoreboard(Guid eventId)
    {
        var battleEvent = await _context.BattleEvents
            .AsNoTracking()
            .FirstOrDefaultAsync(e => e.Id == eventId);

        if (battleEvent is null)
        {
            return NotFound(new { message = "Event not found" });
        }

        // Get active battles (InProgress or RevealCountdown)
        var activeBattles = await _context.Battles
            .Include(b => b.Category)
                .ThenInclude(c => c.Event)
            .Include(b => b.Breaker1)
            .Include(b => b.Breaker2)
            .Include(b => b.Scores)
            .Where(b => b.Category.EventId == eventId && 
                       (b.Status == BattleStatus.InProgress || b.Status == BattleStatus.RevealCountdown))
            .AsNoTracking()
            .ToListAsync();

        // Get recent completed battles (last 10)
        var recentResults = await _context.Battles
            .Include(b => b.Category)
            .Include(b => b.Breaker1)
            .Include(b => b.Breaker2)
            .Include(b => b.Winner)
            .Include(b => b.Scores)
            .Where(b => b.Category.EventId == eventId && 
                       (b.Status == BattleStatus.Completed || b.Status == BattleStatus.Walkover))
            .OrderByDescending(b => b.CompletedAt)
            .Take(10)
            .AsNoTracking()
            .ToListAsync();

        var scoreboard = new ScoreboardResponse
        {
            EventId = eventId,
            EventTitle = battleEvent.Title,
            ActiveBattles = activeBattles.Select(b => MapToLiveBattle(b, battleEvent.JudgeCount)).ToList(),
            RecentResults = recentResults.Select(MapToBattleResult).ToList()
        };

        return Ok(scoreboard);
    }

    /// <summary>
    /// Gets tournament standings by category.
    /// </summary>
    /// <param name="eventId">Event ID.</param>
    /// <returns>Standings for all categories.</returns>
    /// <response code="200">Standings retrieved successfully</response>
    /// <response code="404">Event not found</response>
    [HttpGet("standings")]
    [ProducesResponseType(typeof(StandingsResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetStandings(Guid eventId)
    {
        var eventExists = await _context.BattleEvents.AnyAsync(e => e.Id == eventId);

        if (!eventExists)
        {
            return NotFound(new { message = "Event not found" });
        }

        var categories = await _context.AgeCategories
            .Include(c => c.Registrations)
                .ThenInclude(r => r.Breaker)
            .Include(c => c.Battles)
                .ThenInclude(b => b.Scores)
            .Where(c => c.EventId == eventId)
            .OrderBy(c => c.SortOrder)
            .AsNoTracking()
            .ToListAsync();

        var categoryStandings = categories.Select(c => MapToCategoryStandings(c)).ToList();

        var standings = new StandingsResponse
        {
            EventId = eventId,
            Categories = categoryStandings
        };

        return Ok(standings);
    }

    /// <summary>
    /// Gets live battle details with countdown information.
    /// </summary>
    /// <param name="eventId">Event ID.</param>
    /// <param name="battleId">Battle ID.</param>
    /// <returns>Live battle details.</returns>
    /// <response code="200">Battle details retrieved successfully</response>
    /// <response code="404">Battle not found</response>
    [HttpGet("battles/{battleId:guid}/live")]
    [ProducesResponseType(typeof(LiveBattleResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetLiveBattle(Guid eventId, Guid battleId)
    {
        var battle = await _context.Battles
            .Include(b => b.Category)
                .ThenInclude(c => c.Event)
            .Include(b => b.Breaker1)
            .Include(b => b.Breaker2)
            .Include(b => b.Scores)
            .Where(b => b.Id == battleId && b.Category.EventId == eventId)
            .AsNoTracking()
            .FirstOrDefaultAsync();

        if (battle is null)
        {
            return NotFound(new { message = "Battle not found" });
        }

        var liveBattle = MapToLiveBattle(battle, battle.Category.Event.JudgeCount);

        return Ok(liveBattle);
    }

    #region Helper Methods

    private LiveBattleResponse MapToLiveBattle(Domain.Entities.Battle battle, int judgeCount)
    {
        var judgesScored = battle.Scores
            .Select(s => s.JudgeIdentifier)
            .Distinct()
            .Count();

        return new LiveBattleResponse
        {
            BattleId = battle.Id,
            CategoryName = battle.Category.Name,
            BracketLevel = battle.BracketLevel,
            Breaker1 = new BreakerSummary
            {
                Id = battle.Breaker1.Id,
                Name = battle.Breaker1.Name
            },
            Breaker2 = new BreakerSummary
            {
                Id = battle.Breaker2.Id,
                Name = battle.Breaker2.Name
            },
            Status = battle.Status,
            JudgesScored = judgesScored,
            JudgesTotal = judgeCount,
            RevealAt = battle.Status == BattleStatus.RevealCountdown 
                ? battle.CompletedAt?.AddSeconds(5) // 5-second countdown
                : null
        };
    }

    private BattleResultResponse MapToBattleResult(Domain.Entities.Battle battle)
    {
        var isWalkover = battle.Status == BattleStatus.Walkover;

        var winner = battle.Winner ?? battle.Breaker1;
        var loser = battle.WinnerId == battle.Breaker1Id ? battle.Breaker2 : battle.Breaker1;

        decimal? winnerAvg = null;
        decimal? loserAvg = null;

        if (!isWalkover && battle.Scores.Any())
        {
            var winnerScores = battle.Scores.Where(s => s.BreakerId == winner.Id);
            var loserScores = battle.Scores.Where(s => s.BreakerId == loser.Id);

            winnerAvg = winnerScores.Any() ? _scoringService.CalculateAverageScore(winnerScores) : null;
            loserAvg = loserScores.Any() ? _scoringService.CalculateAverageScore(loserScores) : null;
        }

        return new BattleResultResponse
        {
            BattleId = battle.Id,
            CategoryName = battle.Category.Name,
            BracketLevel = battle.BracketLevel,
            Winner = new BreakerSummary
            {
                Id = winner.Id,
                Name = winner.Name
            },
            Loser = new BreakerSummary
            {
                Id = loser.Id,
                Name = loser.Name
            },
            WinnerAvgScore = winnerAvg,
            LoserAvgScore = loserAvg,
            IsWalkover = isWalkover,
            CompletedAt = battle.CompletedAt ?? DateTime.UtcNow
        };
    }

    private CategoryStandings MapToCategoryStandings(Domain.Entities.AgeCategory category)
    {
        var currentLevel = category.Battles.Any() 
            ? category.Battles.Max(b => b.BracketLevel)
            : (BracketLevel?)null;

        var breakerStandings = category.Registrations
            .Select(r => CalculateBreakerStanding(r, category.Battles))
            .OrderByDescending(s => s.Wins)
            .ThenBy(s => s.Losses)
            .ThenByDescending(s => s.AverageScore)
            .ToList();

        return new CategoryStandings
        {
            CategoryId = category.Id,
            CategoryName = category.Name,
            CurrentPhase = category.CurrentPhase,
            CurrentLevel = currentLevel,
            Standings = breakerStandings
        };
    }

    private BreakerStanding CalculateBreakerStanding(
        Domain.Entities.Registration registration,
        ICollection<Domain.Entities.Battle> battles)
    {
        var breakerBattles = battles
            .Where(b => b.Breaker1Id == registration.BreakerId || b.Breaker2Id == registration.BreakerId)
            .Where(b => b.Status == BattleStatus.Completed || b.Status == BattleStatus.Walkover)
            .ToList();

        var wins = breakerBattles.Count(b => b.WinnerId == registration.BreakerId);
        var losses = breakerBattles.Count - wins;

        // Calculate average score across all battles
        var allScores = breakerBattles
            .SelectMany(b => b.Scores.Where(s => s.BreakerId == registration.BreakerId))
            .ToList();

        decimal? averageScore = allScores.Any() 
            ? _scoringService.CalculateAverageScore(allScores)
            : null;

        var currentLevel = breakerBattles.Any()
            ? breakerBattles.Max(b => b.BracketLevel)
            : (BracketLevel?)null;

        return new BreakerStanding
        {
            BreakerId = registration.BreakerId,
            BreakerName = registration.Breaker.Name,
            CurrentLevel = currentLevel,
            Wins = wins,
            Losses = losses,
            AverageScore = averageScore,
            Status = registration.Status
        };
    }

    #endregion
}
