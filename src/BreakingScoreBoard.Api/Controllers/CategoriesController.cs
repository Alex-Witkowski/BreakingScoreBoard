using BreakingScoreBoard.Api.Contracts;
using BreakingScoreBoard.Api.Contracts.Categories;
using BreakingScoreBoard.Api.Contracts.Events;
using BreakingScoreBoard.Api.Contracts.Registrations;
using BreakingScoreBoard.Api.Infrastructure;
using BreakingScoreBoard.Domain.Entities;
using BreakingScoreBoard.Domain.Enums;
using BreakingScoreBoard.Domain.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace BreakingScoreBoard.Api.Controllers;

/// <summary>
/// Controller for managing age categories within events.
/// </summary>
[ApiController]
[Route("events/{eventId:guid}/categories")]
[Produces("application/json")]
public class CategoriesController : ControllerBase
{
    private readonly BattleDbContext _dbContext;
    private readonly PreSelectionService _preSelectionService;
    private readonly BracketService _bracketService;
    private readonly ILogger<CategoriesController> _logger;
    
    public CategoriesController(
        BattleDbContext dbContext,
        PreSelectionService preSelectionService,
        BracketService bracketService,
        ILogger<CategoriesController> logger)
    {
        _dbContext = dbContext;
        _preSelectionService = preSelectionService;
        _bracketService = bracketService;
        _logger = logger;
    }
    
    /// <summary>
    /// Gets all categories for an event.
    /// </summary>
    /// <param name="eventId">The event ID.</param>
    /// <returns>List of categories.</returns>
    [HttpGet]
    [ProducesResponseType(typeof(IList<CategoryResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<IList<CategoryResponse>>> GetCategories(Guid eventId)
    {
        var eventExists = await _dbContext.BattleEvents
            .AnyAsync(e => e.Id == eventId);
        
        if (!eventExists)
        {
            return NotFound(ErrorResponse.FromMessage("Event not found"));
        }
        
        var categories = await _dbContext.AgeCategories
            .Include(c => c.Registrations)
            .Where(c => c.EventId == eventId)
            .OrderBy(c => c.SortOrder)
            .AsNoTracking()
            .ToListAsync();
        
        return Ok(categories.Select(MapToResponse).ToList());
    }
    
    /// <summary>
    /// Gets a specific category by ID.
    /// </summary>
    /// <param name="eventId">The event ID.</param>
    /// <param name="categoryId">The category ID.</param>
    /// <returns>The category with registrations.</returns>
    [HttpGet("{categoryId:guid}")]
    [ProducesResponseType(typeof(CategoryDetailResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<CategoryDetailResponse>> GetCategory(Guid eventId, Guid categoryId)
    {
        var category = await _dbContext.AgeCategories
            .Include(c => c.Event)
            .Include(c => c.Registrations)
                .ThenInclude(r => r.Breaker)
            .AsNoTracking()
            .FirstOrDefaultAsync(c => c.Id == categoryId && c.EventId == eventId);
        
        if (category is null)
        {
            return NotFound(ErrorResponse.FromMessage("Category not found"));
        }
        
        return Ok(MapToDetailResponse(category));
    }
    
    /// <summary>
    /// Creates a new category for an event.
    /// </summary>
    /// <param name="eventId">The event ID.</param>
    /// <param name="request">The category creation request.</param>
    /// <returns>The created category.</returns>
    [HttpPost]
    [RequireAdminPin]
    [ProducesResponseType(typeof(CategoryResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<CategoryResponse>> CreateCategory(Guid eventId, [FromBody] CreateCategoryRequest request)
    {
        var battleEvent = await _dbContext.BattleEvents
            .Include(e => e.Categories)
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
            return BadRequest(ErrorResponse.FromMessage("Cannot add categories after battles have started"));
        }
        
        // Validate bracket size
        if (request.BracketSize is not (8 or 16 or 32 or 64))
        {
            return BadRequest(ErrorResponse.FromMessage("BracketSize must be 8, 16, 32, or 64"));
        }
        
        // Check for duplicate name
        var nameExists = battleEvent.Categories
            .Any(c => c.Name.Equals(request.Name, StringComparison.OrdinalIgnoreCase));
        
        if (nameExists)
        {
            return BadRequest(ErrorResponse.FromMessage("A category with this name already exists"));
        }
        
        var maxSortOrder = battleEvent.Categories.Any() 
            ? battleEvent.Categories.Max(c => c.SortOrder) 
            : -1;
        
        var category = new AgeCategory
        {
            Id = Guid.NewGuid(),
            EventId = eventId,
            Name = request.Name,
            MaxAge = request.MaxAge,
            BracketSize = request.BracketSize,
            CurrentPhase = CategoryPhase.Registration,
            SortOrder = maxSortOrder + 1
        };
        
        _dbContext.AgeCategories.Add(category);
        await _dbContext.SaveChangesAsync();
        
        _logger.LogInformation("Created category {CategoryId} for event {EventId}", category.Id, eventId);
        
        return CreatedAtAction(
            nameof(GetCategory), 
            new { eventId, categoryId = category.Id }, 
            MapToResponse(category));
    }
    
    /// <summary>
    /// Starts pre-selection for a category when registrations exceed bracket size.
    /// </summary>
    /// <param name="eventId">The event ID.</param>
    /// <param name="categoryId">The category ID.</param>
    /// <returns>Result of pre-selection initiation.</returns>
    [HttpPost("{categoryId:guid}/start-preselection")]
    [RequireAdminPin]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> StartPreSelection(Guid eventId, Guid categoryId)
    {
        var category = await _dbContext.AgeCategories
            .Include(c => c.Registrations)
            .FirstOrDefaultAsync(c => c.Id == categoryId && c.EventId == eventId);
        
        if (category is null)
        {
            return NotFound(ErrorResponse.FromMessage("Category not found"));
        }
        
        // Check if already in pre-selection or later phase
        if (category.CurrentPhase != CategoryPhase.Registration)
        {
            return BadRequest(ErrorResponse.FromMessage("Pre-selection can only be started during registration phase"));
        }
        
        // FR-016: Calculate overflow
        var activeRegistrationCount = category.Registrations
            .Count(r => r.Status == RegistrationStatus.Active);
        
        var overflow = PreSelectionService.CalculateOverflow(activeRegistrationCount, category.BracketSize);
        
        // FR-017: Check if pre-selection is needed
        if (overflow == 0)
        {
            return BadRequest(ErrorResponse.FromMessage($"No pre-selection needed - only {activeRegistrationCount} registrations for {category.BracketSize}-slot bracket"));
        }
        
        // FR-017: Randomly select (2 × overflow) breakers
        var selectedRegistrations = _preSelectionService.SelectBreakersForPreSelection(
            category.Registrations, 
            overflow);
        
        // FR-018: Create pre-selection battles
        var battles = _preSelectionService.CreatePreSelectionBattles(
            selectedRegistrations,
            categoryId,
            overflow);
        
        _dbContext.Battles.AddRange(battles);
        
        // Update category phase to PreSelection (FR-019: blocks new registrations)
        category.CurrentPhase = CategoryPhase.PreSelection;
        
        await _dbContext.SaveChangesAsync();
        
        _logger.LogInformation(
            "Started pre-selection for category {CategoryId}: {Overflow} overflow, {BattleCount} battles created",
            categoryId, overflow, battles.Count);
        
        return Ok(new 
        { 
            message = "Pre-selection started",
            overflow,
            battlesCreated = battles.Count,
            selectedBreakers = selectedRegistrations.Count
        });
    }
    
    /// <summary>
    /// Advances bracket to next level after current level battles are completed.
    /// </summary>
    /// <param name="eventId">The event ID.</param>
    /// <param name="categoryId">The category ID.</param>
    /// <returns>Result of bracket advancement.</returns>
    [HttpPost("{categoryId:guid}/advance-bracket")]
    [RequireAdminPin]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> AdvanceBracket(Guid eventId, Guid categoryId)
    {
        var category = await _dbContext.AgeCategories
            .Include(c => c.Registrations)
            .FirstOrDefaultAsync(c => c.Id == categoryId && c.EventId == eventId);
        
        if (category is null)
        {
            return NotFound(ErrorResponse.FromMessage("Category not found"));
        }
        
        // Determine current bracket level from existing battles
        var currentLevelBattles = await _dbContext.Battles
            .Include(b => b.Winner)
            .Where(b => b.CategoryId == categoryId)
            .OrderByDescending(b => b.BracketLevel)
            .ToListAsync();
        
        if (!currentLevelBattles.Any())
        {
            return BadRequest(ErrorResponse.FromMessage("No battles exist yet. Start with pre-selection or initial bracket setup."));
        }
        
        var currentLevel = currentLevelBattles.First().BracketLevel;
        
        // Get next bracket level
        var nextLevel = BracketService.GetNextBracketLevel(currentLevel);
        if (nextLevel is null)
        {
            return BadRequest(ErrorResponse.FromMessage("Already at Final level - cannot advance further"));
        }
        
        // Get battles from current level only
        var currentRoundBattles = currentLevelBattles
            .Where(b => b.BracketLevel == currentLevel)
            .ToList();
        
        // FR-014: Idempotent check - if next level battles already exist, return success
        var existingNextLevelBattles = await _dbContext.Battles
            .Where(b => b.CategoryId == categoryId && b.BracketLevel == nextLevel)
            .AnyAsync();
        
        if (existingNextLevelBattles)
        {
            return Ok(new { message = "Bracket already advanced to next level", alreadyAdvanced = true });
        }
        
        // Check if all battles in current level are completed
        var incompleteBattles = currentRoundBattles
            .Where(b => b.Status != BattleStatus.Completed && b.Status != BattleStatus.Walkover)
            .ToList();
        
        if (incompleteBattles.Any())
        {
            return BadRequest(ErrorResponse.FromMessage(
                $"Cannot advance - {incompleteBattles.Count} battles still incomplete in {currentLevel}"));
        }
        
        // FR-008: Extract winners from completed battles
        var winners = _bracketService.GetWinnersFromBattles(currentRoundBattles);
        
        if (!winners.Any())
        {
            return BadRequest(ErrorResponse.FromMessage("No completed battles found to advance from"));
        }
        
        // FR-009: Create next round battles (handles bye selection if odd winners)
        var nextRoundBattles = _bracketService.CreateNextRoundBattles(winners, categoryId, nextLevel.Value);
        
        _dbContext.Battles.AddRange(nextRoundBattles);
        
        // Update category phase if advancing to bracket phases
        if (category.CurrentPhase == CategoryPhase.PreSelection && nextLevel == BracketLevel.Top32)
        {
            category.CurrentPhase = CategoryPhase.Bracket;
        }
        
        await _dbContext.SaveChangesAsync();
        
        _logger.LogInformation(
            "Advanced bracket for category {CategoryId}: {WinnerCount} winners from {CurrentLevel} to {NextLevel}, {BattleCount} battles created",
            categoryId, winners.Count, currentLevel, nextLevel, nextRoundBattles.Count);
        
        return Ok(new 
        { 
            message = "Bracket advanced successfully",
            fromLevel = currentLevel.ToString(),
            toLevel = nextLevel.ToString(),
            winners = winners.Count,
            battlesCreated = nextRoundBattles.Count
        });
    }
    
    private static CategoryResponse MapToResponse(AgeCategory category)
    {
        return new CategoryResponse
        {
            Id = category.Id,
            EventId = category.EventId,
            Name = category.Name,
            MaxAge = category.MaxAge,
            BracketSize = category.BracketSize,
            CurrentPhase = category.CurrentPhase,
            SortOrder = category.SortOrder,
            RegistrationCount = category.Registrations.Count
        };
    }

    private static CategoryDetailResponse MapToDetailResponse(AgeCategory category)
    {
        var registrations = category.Registrations
            .Select(r => new RegistrationResponse
            {
                Id = r.Id,
                BreakerId = r.BreakerId,
                BreakerName = r.Breaker.Name,
                Age = r.Breaker.GetAgeAtDate(category.Event.EventDate),
                Status = r.Status,
                RegisteredAt = r.RegisteredAt
            })
            .OrderBy(r => r.BreakerName)
            .ToList();

        return new CategoryDetailResponse
        {
            Id = category.Id,
            Name = category.Name,
            MaxAge = category.MaxAge,
            BracketSize = category.BracketSize,
            Registrations = registrations
        };
    }
}
