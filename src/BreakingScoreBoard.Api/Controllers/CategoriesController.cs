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
            MinBirthYear = request.MinBirthYear,
            MaxBirthYear = request.MaxBirthYear,
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
    /// Starts the initial bracket for a category (when registrations ≤ bracket size).
    /// </summary>
    /// <param name="eventId">The event ID.</param>
    /// <param name="categoryId">The category ID.</param>
    /// <returns>Result of bracket initialization.</returns>
    [HttpPost("{categoryId:guid}/start-bracket")]
    [RequireAdminPin]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> StartBracket(Guid eventId, Guid categoryId)
    {
        var category = await _dbContext.AgeCategories
            .Include(c => c.Registrations)
                .ThenInclude(r => r.Breaker)
            .FirstOrDefaultAsync(c => c.Id == categoryId && c.EventId == eventId);

        if (category is null)
        {
            return NotFound(ErrorResponse.FromMessage("Category not found"));
        }

        // Check if already started
        if (category.CurrentPhase != CategoryPhase.Registration)
        {
            return BadRequest(ErrorResponse.FromMessage("Bracket can only be started during registration phase"));
        }

        // Check if battles already exist
        var existingBattles = await _dbContext.Battles
            .AnyAsync(b => b.CategoryId == categoryId);

        if (existingBattles)
        {
            return BadRequest(ErrorResponse.FromMessage("Battles already exist for this category"));
        }

        // Get active registrations
        var activeRegistrations = category.Registrations
            .Where(r => r.Status == RegistrationStatus.Active)
            .ToList();

        if (activeRegistrations.Count < 2)
        {
            return BadRequest(ErrorResponse.FromMessage($"Cannot start bracket - need at least 2 registrations, have {activeRegistrations.Count}"));
        }

        // Calculate and lock bracket size
        var bracketSize = AgeCategory.CalculateBracketSize(activeRegistrations.Count);
        category.BracketSize = bracketSize;

        // Check if pre-selection is needed instead
        if (activeRegistrations.Count > bracketSize)
        {
            return BadRequest(ErrorResponse.FromMessage($"Cannot start bracket - {activeRegistrations.Count} registrations exceed bracket size {bracketSize}. Use start-preselection instead."));
        }

        // Create initial bracket with random pairing
        var battles = _bracketService.CreateInitialBracket(category, activeRegistrations);

        _dbContext.Battles.AddRange(battles);

        // Update category phase to Bracket
        category.CurrentPhase = CategoryPhase.Bracket;

        await _dbContext.SaveChangesAsync();

        _logger.LogInformation(
            "Started initial bracket for category {CategoryId}: {RegistrationCount} registrations, {BattleCount} battles created",
            categoryId, activeRegistrations.Count, battles.Count);

        return Ok(new
        {
            message = "Bracket started successfully",
            bracketSize,
            registrations = activeRegistrations.Count,
            battlesCreated = battles.Count,
            byesGranted = activeRegistrations.Count - (battles.Count * 2)
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

    /// <summary>
    /// Resets a category by deleting all battles and scores, returning to registration phase.
    /// </summary>
    /// <param name="eventId">The event ID.</param>
    /// <param name="categoryId">The category ID.</param>
    /// <returns>Result of category reset.</returns>
    [HttpPost("{categoryId:guid}/reset")]
    [RequireAdminPin]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> ResetCategory(Guid eventId, Guid categoryId)
    {
        var category = await _dbContext.AgeCategories
            .Include(c => c.Registrations)
            .FirstOrDefaultAsync(c => c.Id == categoryId && c.EventId == eventId);

        if (category is null)
        {
            return NotFound(ErrorResponse.FromMessage("Category not found"));
        }

        // Get all battles for this category
        var battles = await _dbContext.Battles
            .Include(b => b.Scores)
            .Where(b => b.CategoryId == categoryId)
            .ToListAsync();

        var battlesCount = battles.Count;
        var scoresCount = battles.Sum(b => b.Scores.Count);

        // Delete all battles (scores will be cascade deleted)
        _dbContext.Battles.RemoveRange(battles);

        // Reset category phase to Registration
        category.CurrentPhase = CategoryPhase.Registration;

        // Reset all registrations to Active status
        foreach (var registration in category.Registrations)
        {
            registration.Status = RegistrationStatus.Active;
        }

        await _dbContext.SaveChangesAsync();

        _logger.LogInformation(
            "Reset category {CategoryId}: deleted {BattleCount} battles and {ScoreCount} scores",
            categoryId, battlesCount, scoresCount);

        return Ok(new
        {
            message = "Category reset successfully",
            battlesDeleted = battlesCount,
            scoresDeleted = scoresCount,
            currentPhase = CategoryPhase.Registration
        });
    }

    /// <summary>
    /// Deletes a category and all associated data (registrations, battles, scores).
    /// </summary>
    /// <param name="eventId">The event ID.</param>
    /// <param name="categoryId">The category ID.</param>
    /// <returns>Result of category deletion.</returns>
    [HttpDelete("{categoryId:guid}")]
    [RequireAdminPin]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeleteCategory(Guid eventId, Guid categoryId)
    {
        var category = await _dbContext.AgeCategories
            .Include(c => c.Registrations)
            .FirstOrDefaultAsync(c => c.Id == categoryId && c.EventId == eventId);

        if (category is null)
        {
            return NotFound(ErrorResponse.FromMessage("Category not found"));
        }

        // Only allow deletion if category is in Registration phase
        if (category.CurrentPhase != CategoryPhase.Registration)
        {
            return BadRequest(ErrorResponse.FromMessage("Cannot delete category - it must be in Registration phase. Reset category first."));
        }

        // Get all battles for this category to verify none exist
        var battles = await _dbContext.Battles
            .Include(b => b.Scores)
            .Where(b => b.CategoryId == categoryId)
            .ToListAsync();

        // Delete battles and scores if any exist (cleanup from incomplete reset)
        if (battles.Any())
        {
            _dbContext.Battles.RemoveRange(battles);
        }

        var registrationCount = category.Registrations.Count;
        var battleCount = battles.Count;
        var scoreCount = battles.Sum(b => b.Scores.Count);

        // Delete all registrations
        _dbContext.Registrations.RemoveRange(category.Registrations);

        // Delete the category
        _dbContext.AgeCategories.Remove(category);
        
        await _dbContext.SaveChangesAsync();

        _logger.LogInformation(
            "Deleted category {CategoryId} from event {EventId}: {RegistrationCount} registrations, {BattleCount} battles, {ScoreCount} scores",
            categoryId, eventId, registrationCount, battleCount, scoreCount);

        return Ok(new
        {
            message = "Category deleted successfully",
            registrationsDeleted = registrationCount,
            battlesDeleted = battleCount,
            scoresDeleted = scoreCount
        });
    }

    private static CategoryResponse MapToResponse(AgeCategory category)
    {
        var registrationCount = category.Registrations.Count;
        return new CategoryResponse
        {
            Id = category.Id,
            EventId = category.EventId,
            Name = category.Name,
            MaxAge = category.MaxAge,
            MinBirthYear = category.MinBirthYear,
            MaxBirthYear = category.MaxBirthYear,
            BracketSize = AgeCategory.CalculateBracketSize(registrationCount),
            CurrentPhase = category.CurrentPhase,
            SortOrder = category.SortOrder,
            RegistrationCount = registrationCount
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
                BattleName = r.Breaker.DisplayName,
                CategoryId = r.CategoryId,
                CategoryName = category.Name,
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
            BracketSize = AgeCategory.CalculateBracketSize(registrations.Count),
            Registrations = registrations
        };
    }
}
