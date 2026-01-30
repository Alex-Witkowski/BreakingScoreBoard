using BreakingScoreBoard.Api.Contracts;
using BreakingScoreBoard.Api.Contracts.Scores;
using BreakingScoreBoard.Api.Infrastructure;
using BreakingScoreBoard.Domain.Entities;
using BreakingScoreBoard.Domain.Enums;
using BreakingScoreBoard.Domain.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace BreakingScoreBoard.Api.Controllers;

/// <summary>
/// Controller for managing judge scores.
/// </summary>
[ApiController]
[Route("scores")]
[Produces("application/json")]
public class ScoresController : ControllerBase
{
    private readonly BattleDbContext _dbContext;
    private readonly ScoringService _scoringService;
    private readonly ILogger<ScoresController> _logger;
    
    public ScoresController(
        BattleDbContext dbContext,
        ScoringService scoringService,
        ILogger<ScoresController> logger)
    {
        _dbContext = dbContext;
        _scoringService = scoringService;
        _logger = logger;
    }
    
    /// <summary>
    /// Submits judge scores for a battle.
    /// </summary>
    /// <param name="request">The score submission request.</param>
    /// <returns>The created scores.</returns>
    [HttpPost]
    [RequirePin]
    [ProducesResponseType(StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> SubmitScores([FromBody] SubmitScoresRequest request)
    {
        var battle = await _dbContext.Battles
            .Include(b => b.Scores)
            .Include(b => b.Category)
            .FirstOrDefaultAsync(b => b.Id == request.BattleId);
        
        if (battle is null)
        {
            return NotFound(ErrorResponse.FromMessage("Battle not found"));
        }
        
        if (battle.Status != BattleStatus.InProgress)
        {
            return BadRequest(ErrorResponse.FromMessage("Battle is not in progress"));
        }
        
        // Check if scores are already locked
        var existingScoresLocked = battle.Scores
            .Any(s => s.JudgeIdentifier == request.JudgeIdentifier && s.IsLocked);
        
        if (existingScoresLocked)
        {
            return StatusCode(StatusCodes.Status403Forbidden, 
                ErrorResponse.FromMessage("Scores are locked and cannot be modified"));
        }
        
        // Validate score range (0-100)
        if (request.Breaker1Score < 0 || request.Breaker1Score > 100 ||
            request.Breaker2Score < 0 || request.Breaker2Score > 100)
        {
            return BadRequest(ErrorResponse.FromMessage("Scores must be between 0 and 100"));
        }
        
        // If re-battle, enforce differentiation
        if (battle.IsReBattle && request.Breaker1Score == request.Breaker2Score)
        {
            return BadRequest(ErrorResponse.FromMessage("In re-battles, scores for both breakers must be different"));
        }
        
        var now = DateTime.UtcNow;
        
        // Remove existing scores from this judge (if resubmitting)
        var existingScores = battle.Scores
            .Where(s => s.JudgeIdentifier == request.JudgeIdentifier)
            .ToList();
        
        _dbContext.JudgeScores.RemoveRange(existingScores);
        
        // Create new scores
        var score1 = new JudgeScore
        {
            Id = Guid.NewGuid(),
            BattleId = request.BattleId,
            BreakerId = battle.Breaker1Id,
            JudgeIdentifier = request.JudgeIdentifier,
            Score = request.Breaker1Score,
            SubmittedAt = now,
            IsLocked = false
        };
        
        var score2 = new JudgeScore
        {
            Id = Guid.NewGuid(),
            BattleId = request.BattleId,
            BreakerId = battle.Breaker2Id,
            JudgeIdentifier = request.JudgeIdentifier,
            Score = request.Breaker2Score,
            SubmittedAt = now,
            IsLocked = false
        };
        
        _dbContext.JudgeScores.AddRange(score1, score2);
        await _dbContext.SaveChangesAsync();
        
        _logger.LogInformation(
            "Judge {JudgeIdentifier} submitted scores for battle {BattleId}: Breaker1={Score1}, Breaker2={Score2}",
            request.JudgeIdentifier, request.BattleId, request.Breaker1Score, request.Breaker2Score);
        
        return CreatedAtAction(
            nameof(GetScoresByBattle),
            new { battleId = request.BattleId },
            new { message = "Scores submitted successfully" });
    }
    
    /// <summary>
    /// Gets all scores for a battle.
    /// </summary>
    /// <param name="battleId">The battle ID.</param>
    /// <returns>List of scores.</returns>
    [HttpGet("battle/{battleId:guid}")]
    [ProducesResponseType(typeof(IList<JudgeScoreResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<IList<JudgeScoreResponse>>> GetScoresByBattle(Guid battleId)
    {
        var battleExists = await _dbContext.Battles.AnyAsync(b => b.Id == battleId);
        
        if (!battleExists)
        {
            return NotFound(ErrorResponse.FromMessage("Battle not found"));
        }
        
        var scores = await _dbContext.JudgeScores
            .Where(s => s.BattleId == battleId)
            .OrderBy(s => s.JudgeIdentifier)
            .ThenBy(s => s.SubmittedAt)
            .AsNoTracking()
            .ToListAsync();
        
        return Ok(scores.Select(s => new JudgeScoreResponse
        {
            Id = s.Id,
            BattleId = s.BattleId,
            BreakerId = s.BreakerId,
            JudgeIdentifier = s.JudgeIdentifier,
            Score = s.Score,
            IsLocked = s.IsLocked,
            SubmittedAt = s.SubmittedAt
        }).ToList());
    }
}
