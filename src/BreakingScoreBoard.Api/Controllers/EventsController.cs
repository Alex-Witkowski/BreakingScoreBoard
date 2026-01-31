using BreakingScoreBoard.Api.Contracts;
using BreakingScoreBoard.Api.Contracts.Categories;
using BreakingScoreBoard.Api.Contracts.Events;
using BreakingScoreBoard.Api.Infrastructure;
using BreakingScoreBoard.Domain.Entities;
using BreakingScoreBoard.Domain.Enums;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace BreakingScoreBoard.Api.Controllers;

/// <summary>
/// Controller for managing battle events.
/// </summary>
[ApiController]
[Route("events")]
[Produces("application/json")]
public class EventsController : ControllerBase
{
    private readonly BattleDbContext _dbContext;
    private readonly ILogger<EventsController> _logger;

    public EventsController(BattleDbContext dbContext, ILogger<EventsController> logger)
    {
        _dbContext = dbContext;
        _logger = logger;
    }

    /// <summary>
    /// Creates a new battle event.
    /// </summary>
    /// <param name="request">The event creation request.</param>
    /// <returns>The created event.</returns>
    [HttpPost]
    [ProducesResponseType(typeof(EventResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<EventResponse>> CreateEvent([FromBody] CreateEventRequest request)
    {
        // Validate judge count
        if (request.JudgeCount != 3 && request.JudgeCount != 5)
        {
            return BadRequest(ErrorResponse.FromMessage("JudgeCount must be 3 or 5"));
        }

        // Validate admin and judge PINs are different
        if (request.AdminPin == request.JudgePin)
        {
            return BadRequest(ErrorResponse.FromMessage("AdminPin and JudgePin must be different"));
        }

        // Validate categories
        if (request.Categories is not null)
        {
            foreach (var category in request.Categories)
            {
                if (category.BracketSize is not (8 or 16 or 32 or 64))
                {
                    return BadRequest(ErrorResponse.FromMessage($"BracketSize must be 8, 16, 32, or 64. Got: {category.BracketSize}"));
                }
            }

            // Check for duplicate category names
            var categoryNames = request.Categories.Select(c => c.Name.ToLowerInvariant()).ToList();
            if (categoryNames.Count != categoryNames.Distinct().Count())
            {
                return BadRequest(ErrorResponse.FromMessage("Category names must be unique within an event"));
            }
        }

        var now = DateTime.UtcNow;
        var battleEvent = new BattleEvent
        {
            Id = Guid.NewGuid(),
            Title = request.Title,
            EventDate = request.EventDate,
            Location = request.Location,
            JudgeCount = request.JudgeCount,
            AdminPinHash = PinHasher.Hash(request.AdminPin),
            JudgePinHash = PinHasher.Hash(request.JudgePin),
            RegistrationOpen = true,
            CreatedAt = now,
            UpdatedAt = now
        };

        // Add categories if provided
        if (request.Categories is not null)
        {
            var sortOrder = 0;
            foreach (var categoryRequest in request.Categories)
            {
                var category = new AgeCategory
                {
                    Id = Guid.NewGuid(),
                    EventId = battleEvent.Id,
                    Name = categoryRequest.Name,
                    MaxAge = categoryRequest.MaxAge,
                    BracketSize = categoryRequest.BracketSize,
                    CurrentPhase = CategoryPhase.Registration,
                    SortOrder = sortOrder++
                };
                battleEvent.Categories.Add(category);
            }
        }

        _dbContext.BattleEvents.Add(battleEvent);
        await _dbContext.SaveChangesAsync();

        _logger.LogInformation("Created event {EventId} with title {Title}", battleEvent.Id, battleEvent.Title);

        var response = MapToResponse(battleEvent);
        return CreatedAtAction(nameof(GetEvent), new { eventId = battleEvent.Id }, response);
    }

    /// <summary>
    /// Gets all events.
    /// </summary>
    /// <returns>A list of all events.</returns>
    [HttpGet]
    [ProducesResponseType(typeof(List<EventResponse>), StatusCodes.Status200OK)]
    public async Task<ActionResult<List<EventResponse>>> GetAllEvents()
    {
        var events = await _dbContext.BattleEvents
            .Include(e => e.Categories)
            .ThenInclude(c => c.Registrations)
            .AsSplitQuery()
            .AsNoTracking()
            .OrderByDescending(e => e.EventDate)
            .ToListAsync();

        var response = events.Select(MapToResponse).ToList();
        return Ok(response);
    }

    /// <summary>
    /// Gets an event by ID.
    /// </summary>
    /// <param name="eventId">The event ID.</param>
    /// <returns>The event.</returns>
    [HttpGet("{eventId:guid}")]
    [ProducesResponseType(typeof(EventResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<EventResponse>> GetEvent(Guid eventId)
    {
        var battleEvent = await _dbContext.BattleEvents
            .Include(e => e.Categories)
            .ThenInclude(c => c.Registrations)
            .AsSplitQuery()
            .AsNoTracking()
            .FirstOrDefaultAsync(e => e.Id == eventId);

        if (battleEvent is null)
        {
            return NotFound(ErrorResponse.FromMessage("Event not found"));
        }

        return Ok(MapToResponse(battleEvent));
    }

    /// <summary>
    /// Updates an existing event.
    /// </summary>
    /// <param name="eventId">The event ID.</param>
    /// <param name="request">The update request.</param>
    /// <returns>The updated event.</returns>
    [HttpPatch("{eventId:guid}")]
    [RequireAdminPin]
    [ProducesResponseType(typeof(EventResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<EventResponse>> UpdateEvent(Guid eventId, [FromBody] UpdateEventRequest request)
    {
        var battleEvent = await _dbContext.BattleEvents
            .Include(e => e.Categories)
            .ThenInclude(c => c.Registrations)
            .AsSplitQuery()
            .FirstOrDefaultAsync(e => e.Id == eventId);

        if (battleEvent is null)
        {
            return NotFound(ErrorResponse.FromMessage("Event not found"));
        }

        // Check if any battles have started (FR-013)
        var hasBattles = await _dbContext.Battles
            .AnyAsync(b => b.Category.EventId == eventId);

        if (hasBattles)
        {
            return BadRequest(ErrorResponse.FromMessage("Cannot modify event after battles have started"));
        }

        // Apply updates
        if (request.Title is not null)
        {
            battleEvent.Title = request.Title;
        }

        if (request.EventDate.HasValue)
        {
            battleEvent.EventDate = request.EventDate.Value;
        }

        if (request.Location is not null)
        {
            battleEvent.Location = request.Location;
        }

        if (request.RegistrationOpen.HasValue)
        {
            battleEvent.RegistrationOpen = request.RegistrationOpen.Value;
        }

        battleEvent.UpdatedAt = DateTime.UtcNow;

        await _dbContext.SaveChangesAsync();

        _logger.LogInformation("Updated event {EventId}", eventId);

        return Ok(MapToResponse(battleEvent));
    }

    /// <summary>
    /// Regenerates the judge PIN for an event.
    /// </summary>
    /// <param name="eventId">The event ID.</param>
    /// <param name="request">The new judge PIN.</param>
    /// <returns>Success response.</returns>
    [HttpPost("{eventId:guid}/regenerate-judge-pin")]
    [RequireAdminPin]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> RegenerateJudgePin(Guid eventId, [FromBody] RegenerateJudgePinRequest request)
    {
        var battleEvent = await _dbContext.BattleEvents
            .FirstOrDefaultAsync(e => e.Id == eventId);

        if (battleEvent is null)
        {
            return NotFound(ErrorResponse.FromMessage("Event not found"));
        }

        var newPinHash = PinHasher.Hash(request.NewJudgePin);

        // Ensure new judge PIN is different from admin PIN
        if (newPinHash == battleEvent.AdminPinHash)
        {
            return BadRequest(ErrorResponse.FromMessage("JudgePin cannot be the same as AdminPin"));
        }

        battleEvent.JudgePinHash = newPinHash;
        battleEvent.UpdatedAt = DateTime.UtcNow;

        await _dbContext.SaveChangesAsync();

        _logger.LogInformation("Regenerated judge PIN for event {EventId}", eventId);

        return NoContent();
    }

    /// <summary>
    /// Deletes an event.
    /// </summary>
    /// <param name="eventId">The event ID.</param>
    /// <returns>Success response.</returns>
    [HttpDelete("{eventId:guid}")]
    [RequireAdminPin]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeleteEvent(Guid eventId)
    {
        var battleEvent = await _dbContext.BattleEvents
            .FirstOrDefaultAsync(e => e.Id == eventId);

        if (battleEvent is null)
        {
            return NotFound(ErrorResponse.FromMessage("Event not found"));
        }

        _dbContext.BattleEvents.Remove(battleEvent);
        await _dbContext.SaveChangesAsync();

        _logger.LogInformation("Deleted event {EventId} with title {Title}", eventId, battleEvent.Title);

        return NoContent();
    }

    private static EventResponse MapToResponse(BattleEvent battleEvent)
    {
        return new EventResponse
        {
            Id = battleEvent.Id,
            Title = battleEvent.Title,
            EventDate = battleEvent.EventDate,
            Location = battleEvent.Location,
            JudgeCount = battleEvent.JudgeCount,
            RegistrationOpen = battleEvent.RegistrationOpen,
            CreatedAt = battleEvent.CreatedAt,
            UpdatedAt = battleEvent.UpdatedAt,
            Categories = battleEvent.Categories
                .OrderBy(c => c.SortOrder)
                .Select(c => new CategoryResponse
                {
                    Id = c.Id,
                    EventId = c.EventId,
                    Name = c.Name,
                    MaxAge = c.MaxAge,
                    BracketSize = c.BracketSize,
                    CurrentPhase = c.CurrentPhase,
                    SortOrder = c.SortOrder,
                    RegistrationCount = c.Registrations.Count
                })
                .ToList()
        };
    }
}

/// <summary>
/// Request model for regenerating the judge PIN.
/// </summary>
public record RegenerateJudgePinRequest
{
    /// <summary>
    /// The new judge PIN.
    /// </summary>
    public required string NewJudgePin { get; init; }
}
