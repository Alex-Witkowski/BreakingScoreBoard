using BreakingScoreBoard.Api.Contracts;
using BreakingScoreBoard.Api.Contracts.Battles;
using BreakingScoreBoard.Api.Contracts.Scores;
using BreakingScoreBoard.Api.Infrastructure;
using BreakingScoreBoard.Domain.Enums;
using BreakingScoreBoard.Domain.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace BreakingScoreBoard.Api.Controllers;

/// <summary>
/// Controller for managing battles.
/// </summary>
[ApiController]
[Route("battles")]
[Produces("application/json")]
public class BattlesController : ControllerBase
{
    private readonly BattleDbContext _dbContext;
    private readonly ScoringService _scoringService;
    private readonly ILogger<BattlesController> _logger;
    
    public BattlesController(
        BattleDbContext dbContext,
        ScoringService scoringService,
        ILogger<BattlesController> logger)
    {
        _dbContext = dbContext;
        _scoringService = scoringService;
        _logger = logger;
    }
    
    /// <summary>
    /// Gets all battles for a category.
    /// </summary>
    /// <param name="categoryId">The category ID.</param>
    /// <returns>List of battles.</returns>
    [HttpGet]
    [ProducesResponseType(typeof(IList<BattleResponse>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IList<BattleResponse>>> GetBattles([FromQuery] Guid? categoryId)
    {
        var query = _dbContext.Battles
            .Include(b => b.Breaker1)
            .Include(b => b.Breaker2)
            .Include(b => b.Winner)
            .AsNoTracking();
        
        if (categoryId.HasValue)
        {
            query = query.Where(b => b.CategoryId == categoryId.Value);
        }
        
        var battles = await query
            .OrderBy(b => b.BracketLevel)
            .ThenBy(b => b.BracketPosition)
            .ToListAsync();
        
        return Ok(battles.Select(MapToResponse).ToList());
    }
    
    /// <summary>
    /// Gets a specific battle with detailed scoring information.
    /// </summary>
    /// <param name="battleId">The battle ID.</param>
    /// <returns>The battle with scores.</returns>
    [HttpGet("{battleId:guid}")]
    [ProducesResponseType(typeof(BattleDetailResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<BattleDetailResponse>> GetBattle(Guid battleId)
    {
        var battle = await _dbContext.Battles
            .Include(b => b.Breaker1)
            .Include(b => b.Breaker2)
            .Include(b => b.Winner)
            .Include(b => b.Scores)
            .AsNoTracking()
            .FirstOrDefaultAsync(b => b.Id == battleId);
        
        if (battle is null)
        {
            return NotFound(ErrorResponse.FromMessage("Battle not found"));
        }
        
        return Ok(MapToDetailResponse(battle));
    }
    
    /// <summary>
    /// Starts a battle (changes status to InProgress).
    /// </summary>
    /// <param name="battleId">The battle ID.</param>
    /// <returns>The updated battle.</returns>
    [HttpPost("{battleId:guid}/start")]
    [RequireAdminPin]
    [ProducesResponseType(typeof(BattleResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<BattleResponse>> StartBattle(Guid battleId)
    {
        var battle = await _dbContext.Battles
            .Include(b => b.Breaker1)
            .Include(b => b.Breaker2)
            .FirstOrDefaultAsync(b => b.Id == battleId);
        
        if (battle is null)
        {
            return NotFound(ErrorResponse.FromMessage("Battle not found"));
        }
        
        if (battle.Status != BattleStatus.Scheduled)
        {
            return BadRequest(ErrorResponse.FromMessage($"Battle cannot be started from status {battle.Status}"));
        }
        
        battle.Status = BattleStatus.InProgress;
        await _dbContext.SaveChangesAsync();
        
        _logger.LogInformation("Started battle {BattleId}", battleId);
        
        return Ok(MapToResponse(battle));
    }
    
    /// <summary>
    /// Triggers the reveal countdown for a battle (locks scores).
    /// </summary>
    /// <param name="battleId">The battle ID.</param>
    /// <returns>The updated battle.</returns>
    [HttpPost("{battleId:guid}/reveal")]
    [RequireAdminPin]
    [ProducesResponseType(typeof(BattleDetailResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<BattleDetailResponse>> TriggerReveal(Guid battleId)
    {
        var battle = await _dbContext.Battles
            .Include(b => b.Breaker1)
            .Include(b => b.Breaker2)
            .Include(b => b.Scores)
            .Include(b => b.Category)
            .FirstOrDefaultAsync(b => b.Id == battleId);
        
        if (battle is null)
        {
            return NotFound(ErrorResponse.FromMessage("Battle not found"));
        }
        
        if (battle.Status != BattleStatus.InProgress)
        {
            return BadRequest(ErrorResponse.FromMessage($"Reveal can only be triggered from InProgress status"));
        }
        
        // Get expected judge count from event
        var battleEvent = await _dbContext.BattleEvents
            .FirstOrDefaultAsync(e => e.Id == battle.Category.EventId);
        
        var expectedJudgeCount = battleEvent?.JudgeCount ?? 3;
        
        // Check if all judges have scored
        var uniqueJudges = battle.Scores.Select(s => s.JudgeIdentifier).Distinct().Count();
        if (uniqueJudges < expectedJudgeCount)
        {
            return BadRequest(ErrorResponse.FromMessage($"Cannot reveal: only {uniqueJudges} of {expectedJudgeCount} judges have scored"));
        }
        
        // Lock all scores
        foreach (var score in battle.Scores)
        {
            score.IsLocked = true;
        }
        
        battle.Status = BattleStatus.RevealCountdown;
        await _dbContext.SaveChangesAsync();
        
        _logger.LogInformation("Triggered reveal countdown for battle {BattleId}", battleId);
        
        return Ok(MapToDetailResponse(battle));
    }
    
    /// <summary>
    /// Records a walkover (one breaker no-show, organizer selects winner).
    /// </summary>
    /// <param name="battleId">The battle ID.</param>
    /// <param name="request">Walkover request with winner ID.</param>
    /// <returns>Result of walkover recording.</returns>
    [HttpPost("{battleId:guid}/walkover")]
    [RequireAdminPin]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> RecordWalkover(Guid battleId, [FromBody] WalkoverRequest request)
    {
        var battle = await _dbContext.Battles
            .Include(b => b.Scores)
            .Include(b => b.Breaker1)
            .Include(b => b.Breaker2)
            .Include(b => b.Category)
                .ThenInclude(c => c.Event)
            .FirstOrDefaultAsync(b => b.Id == battleId);
        
        if (battle is null)
        {
            return NotFound(ErrorResponse.FromMessage("Battle not found"));
        }
        
        // FR-027: Validate winner is a participant
        if (request.WinnerId != battle.Breaker1Id && request.WinnerId != battle.Breaker2Id)
        {
            return BadRequest(ErrorResponse.FromMessage("Winner must be one of the battle participants"));
        }
        
        // FR-028: Cannot record walkover if scores already submitted
        if (battle.Scores.Any())
        {
            return BadRequest(ErrorResponse.FromMessage("Cannot record walkover - scores already submitted"));
        }
        
        // Record walkover
        battle.WinnerId = request.WinnerId;
        battle.Status = BattleStatus.Walkover;
        battle.CompletedAt = DateTime.UtcNow;
        
        await _dbContext.SaveChangesAsync();
        
        _logger.LogInformation("Recorded walkover for battle {BattleId}, winner: {WinnerId}", battleId, request.WinnerId);
        
        return Ok(new 
        { 
            message = "Walkover recorded successfully",
            battleId,
            winnerId = request.WinnerId,
            status = BattleStatus.Walkover
        });
    }
    
    private static BattleResponse MapToResponse(Domain.Entities.Battle battle)
    {
        return new BattleResponse
        {
            Id = battle.Id,
            CategoryId = battle.CategoryId,
            BracketLevel = battle.BracketLevel,
            BracketPosition = battle.BracketPosition,
            Breaker1Id = battle.Breaker1Id,
            Breaker1Name = battle.Breaker1.Name,
            Breaker2Id = battle.Breaker2Id,
            Breaker2Name = battle.Breaker2.Name,
            WinnerId = battle.WinnerId,
            WinnerName = battle.Winner?.Name,
            Status = battle.Status,
            IsReBattle = battle.IsReBattle,
            CompletedAt = battle.CompletedAt
        };
    }
    
    private BattleDetailResponse MapToDetailResponse(Domain.Entities.Battle battle)
    {
        var breaker1Scores = battle.Scores.Where(s => s.BreakerId == battle.Breaker1Id);
        var breaker2Scores = battle.Scores.Where(s => s.BreakerId == battle.Breaker2Id);
        
        return new BattleDetailResponse
        {
            Id = battle.Id,
            CategoryId = battle.CategoryId,
            BracketLevel = battle.BracketLevel,
            BracketPosition = battle.BracketPosition,
            Breaker1Id = battle.Breaker1Id,
            Breaker1Name = battle.Breaker1.Name,
            Breaker2Id = battle.Breaker2Id,
            Breaker2Name = battle.Breaker2.Name,
            WinnerId = battle.WinnerId,
            WinnerName = battle.Winner?.Name,
            Status = battle.Status,
            IsReBattle = battle.IsReBattle,
            Breaker1AverageScore = _scoringService.CalculateAverageScore(breaker1Scores),
            Breaker2AverageScore = _scoringService.CalculateAverageScore(breaker2Scores),
            Scores = battle.Scores
                .OrderBy(s => s.JudgeIdentifier)
                .ThenBy(s => s.SubmittedAt)
                .Select(s => new JudgeScoreResponse
                {
                    Id = s.Id,
                    BattleId = s.BattleId,
                    BreakerId = s.BreakerId,
                    JudgeIdentifier = s.JudgeIdentifier,
                    Score = s.Score,
                    IsLocked = s.IsLocked,
                    SubmittedAt = s.SubmittedAt
                })
                .ToList(),
            CompletedAt = battle.CompletedAt
        };
    }
}
