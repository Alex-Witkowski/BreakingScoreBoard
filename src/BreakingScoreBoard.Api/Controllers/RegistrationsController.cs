using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using BreakingScoreBoard.Api.Contracts.Registrations;
using BreakingScoreBoard.Api.Infrastructure;
using BreakingScoreBoard.Domain.Entities;
using BreakingScoreBoard.Domain.Enums;

namespace BreakingScoreBoard.Api.Controllers;

/// <summary>
/// Manages breaker registrations for event categories.
/// </summary>
[ApiController]
[Produces("application/json")]
public class RegistrationsController : ControllerBase
{
    private readonly BattleDbContext _context;

    public RegistrationsController(BattleDbContext context)
    {
        _context = context;
    }

    /// <summary>
    /// Register a breaker for a category.
    /// </summary>
    /// <param name="eventId">Event ID</param>
    /// <param name="categoryId">Category ID</param>
    /// <param name="request">Registration details</param>
    /// <returns>Created registration</returns>
    /// <response code="201">Registration created successfully</response>
    /// <response code="400">Invalid age or birth date</response>
    /// <response code="404">Event or category not found</response>
    [HttpPost("events/{eventId:guid}/categories/{categoryId:guid}/registrations")]
    [ProducesResponseType(typeof(RegistrationResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> RegisterBreaker(
        Guid eventId,
        Guid categoryId,
        [FromBody] RegisterBreakerRequest request)
    {
        // Validate event exists
        var battleEvent = await _context.BattleEvents
            .FirstOrDefaultAsync(e => e.Id == eventId);

        if (battleEvent is null)
        {
            return NotFound(new { message = "Event not found" });
        }

        // Validate category exists and belongs to event
        var category = await _context.AgeCategories
            .FirstOrDefaultAsync(c => c.Id == categoryId && c.EventId == eventId);

        if (category is null)
        {
            return NotFound(new { message = "Category not found" });
        }

        // FR-019: Block registrations if category is not in Registration phase
        if (category.CurrentPhase != CategoryPhase.Registration)
        {
            return BadRequest(new
            {
                message = "Registration closed - category has moved to pre-selection or bracket phase",
                currentPhase = category.CurrentPhase.ToString()
            });
        }

        // FR-002: Validate birth date is in the past
        if (!Breaker.IsValidBirthDate(request.BirthDate))
        {
            return BadRequest(new
            {
                message = "Birth date must be in the past",
                field = "birthDate"
            });
        }

        // Calculate age at event date
        var age = Breaker.GetAgeAtDate(request.BirthDate, battleEvent.EventDate);

        // FR-002: Validate age fits category
        if (category.MaxAge.HasValue && age > category.MaxAge.Value)
        {
            return BadRequest(new
            {
                message = $"Breaker age ({age}) exceeds category maximum age ({category.MaxAge.Value})",
                age,
                maxAge = category.MaxAge.Value,
                field = "birthDate"
            });
        }

        // FR-012: Check for existing breaker (by name and birth date)
        var existingBreaker = await _context.Breakers
            .FirstOrDefaultAsync(b => b.Name == request.Name && b.BirthDate == request.BirthDate);

        Breaker breaker;
        if (existingBreaker is not null)
        {
            // Breaker already exists, update display name if provided
            breaker = existingBreaker;
            if (!string.IsNullOrWhiteSpace(request.BattleName))
            {
                breaker.DisplayName = request.BattleName;
            }
        }
        else
        {
            // Create new breaker
            breaker = new Breaker
            {
                Id = Guid.NewGuid(),
                Name = request.Name,
                DisplayName = request.BattleName,
                BirthDate = request.BirthDate
            };
            _context.Breakers.Add(breaker);
        }

        // FR-012: Check for duplicate registration in this category
        var existingRegistration = await _context.Registrations
            .FirstOrDefaultAsync(r =>
                r.BreakerId == breaker.Id &&
                r.CategoryId == categoryId);

        Registration registration;
        if (existingRegistration is not null)
        {
            // Duplicate registration - update timestamp (idempotent)
            existingRegistration.RegisteredAt = DateTime.UtcNow;
            registration = existingRegistration;
        }
        else
        {
            // Create new registration
            registration = new Registration
            {
                Id = Guid.NewGuid(),
                BreakerId = breaker.Id,
                CategoryId = categoryId,
                Status = RegistrationStatus.Active,
                RegisteredAt = DateTime.UtcNow
            };
            _context.Registrations.Add(registration);
        }

        await _context.SaveChangesAsync();

        var response = new RegistrationResponse
        {
            Id = registration.Id,
            BreakerId = breaker.Id,
            BreakerName = breaker.Name,
            BattleName = breaker.DisplayName,
            CategoryId = categoryId,
            CategoryName = category.Name,
            Age = age,
            Status = registration.Status,
            RegisteredAt = registration.RegisteredAt
        };

        return CreatedAtAction(
            nameof(GetRegistration),
            new { eventId, categoryId, breakerId = breaker.Id },
            response);
    }

    /// <summary>
    /// Get a specific registration.
    /// </summary>
    /// <param name="eventId">Event ID</param>
    /// <param name="categoryId">Category ID</param>
    /// <param name="breakerId">Breaker ID</param>
    /// <returns>Registration details</returns>
    /// <response code="200">Registration found</response>
    /// <response code="404">Registration not found</response>
    [HttpGet("events/{eventId:guid}/categories/{categoryId:guid}/registrations/{breakerId:guid}", Name = nameof(GetRegistration))]
    [ProducesResponseType(typeof(RegistrationResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetRegistration(
        Guid eventId,
        Guid categoryId,
        Guid breakerId)
    {
        var registration = await _context.Registrations
            .Include(r => r.Breaker)
            .Include(r => r.Category)
            .ThenInclude(c => c.Event)
            .FirstOrDefaultAsync(r =>
                r.BreakerId == breakerId &&
                r.CategoryId == categoryId &&
                r.Category.EventId == eventId);

        if (registration is null)
        {
            return NotFound(new { message = "Registration not found" });
        }

        var age = registration.Breaker.GetAgeAtDate(registration.Category.Event.EventDate);

        var response = new RegistrationResponse
        {
            Id = registration.Id,
            BreakerId = registration.BreakerId,
            BreakerName = registration.Breaker.Name,
            BattleName = registration.Breaker.DisplayName,
            CategoryId = registration.CategoryId,
            CategoryName = registration.Category.Name,
            Age = age,
            Status = registration.Status,
            RegisteredAt = registration.RegisteredAt
        };

        return Ok(response);
    }

    /// <summary>
    /// Get all registrations for an event across all categories.
    /// </summary>
    /// <param name="eventId">Event ID</param>
    /// <returns>List of registrations for the event</returns>
    /// <response code="200">List of registrations</response>
    /// <response code="404">Event not found</response>
    [HttpGet("events/{eventId:guid}/registrations")]
    [ProducesResponseType(typeof(List<RegistrationResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetEventRegistrations(Guid eventId)
    {
        // Validate event exists
        var battleEvent = await _context.BattleEvents
            .FirstOrDefaultAsync(e => e.Id == eventId);

        if (battleEvent is null)
        {
            return NotFound(new { message = "Event not found" });
        }

        var registrations = await _context.Registrations
            .Include(r => r.Breaker)
            .Include(r => r.Category)
            .ThenInclude(c => c.Event)
            .Where(r => r.Category.EventId == eventId && r.Category.CurrentPhase == CategoryPhase.Registration)
            .OrderByDescending(r => r.RegisteredAt)
            .Select(r => new RegistrationResponse
            {
                Id = r.Id,
                BreakerId = r.BreakerId,
                BreakerName = r.Breaker.Name,
                BattleName = r.Breaker.DisplayName,
                CategoryId = r.CategoryId,
                CategoryName = r.Category.Name,
                Age = r.Breaker.GetAgeAtDate(battleEvent.EventDate),
                Status = r.Status,
                RegisteredAt = r.RegisteredAt
            })
            .ToListAsync();

        return Ok(registrations);
    }}