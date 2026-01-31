using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using BreakingScoreBoard.Api.Contracts.Breakers;
using BreakingScoreBoard.Api.Infrastructure;

namespace BreakingScoreBoard.Api.Controllers;

/// <summary>
/// Manages breaker (dancer) information.
/// </summary>
[ApiController]
[Produces("application/json")]
public class BreakersController : ControllerBase
{
    private readonly BattleDbContext _context;
    private readonly ILogger<BreakersController> _logger;

    public BreakersController(BattleDbContext context, ILogger<BreakersController> logger)
    {
        _context = context;
        _logger = logger;
    }

    /// <summary>
    /// Get a breaker by ID.
    /// </summary>
    /// <param name="breakerId">Breaker ID</param>
    /// <returns>Breaker details</returns>
    /// <response code="200">Breaker found</response>
    /// <response code="404">Breaker not found</response>
    [HttpGet("breakers/{breakerId:guid}")]
    [ProducesResponseType(typeof(BreakerResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetBreaker(Guid breakerId)
    {
        var breaker = await _context.Breakers
            .FirstOrDefaultAsync(b => b.Id == breakerId);

        if (breaker is null)
        {
            return NotFound(new { message = "Breaker not found" });
        }

        var response = new BreakerResponse
        {
            Id = breaker.Id,
            Name = breaker.Name,
            BattleName = breaker.DisplayName,
            BirthDate = breaker.BirthDate,
            CreatedAt = breaker.CreatedAt
        };

        return Ok(response);
    }

    /// <summary>
    /// Update a breaker's information.
    /// </summary>
    /// <param name="breakerId">Breaker ID</param>
    /// <param name="request">Updated breaker information</param>
    /// <returns>Updated breaker details</returns>
    /// <response code="200">Breaker updated successfully</response>
    /// <response code="404">Breaker not found</response>
    [HttpPut("breakers/{breakerId:guid}")]
    [ProducesResponseType(typeof(BreakerResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UpdateBreaker(
        Guid breakerId,
        [FromBody] UpdateBreakerRequest request)
    {
        var breaker = await _context.Breakers
            .FirstOrDefaultAsync(b => b.Id == breakerId);

        if (breaker is null)
        {
            return NotFound(new { message = "Breaker not found" });
        }

        // Update breaker information
        breaker.Name = request.Name;
        breaker.DisplayName = request.BattleName;
        breaker.BirthDate = request.BirthDate;

        await _context.SaveChangesAsync();

        _logger.LogInformation(
            "Updated breaker {BreakerId}: Name={Name}, BattleName={BattleName}",
            breakerId,
            request.Name,
            request.BattleName);

        var response = new BreakerResponse
        {
            Id = breaker.Id,
            Name = breaker.Name,
            BattleName = breaker.DisplayName,
            BirthDate = breaker.BirthDate,
            CreatedAt = breaker.CreatedAt
        };

        return Ok(response);
    }
}
