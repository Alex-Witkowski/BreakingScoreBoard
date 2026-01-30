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
/// Controller for managing age categories within events.
/// </summary>
[ApiController]
[Route("events/{eventId:guid}/categories")]
[Produces("application/json")]
public class CategoriesController : ControllerBase
{
    private readonly BattleDbContext _dbContext;
    private readonly ILogger<CategoriesController> _logger;
    
    public CategoriesController(BattleDbContext dbContext, ILogger<CategoriesController> logger)
    {
        _dbContext = dbContext;
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
    /// <returns>The category.</returns>
    [HttpGet("{categoryId:guid}")]
    [ProducesResponseType(typeof(CategoryResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<CategoryResponse>> GetCategory(Guid eventId, Guid categoryId)
    {
        var category = await _dbContext.AgeCategories
            .Include(c => c.Registrations)
            .AsNoTracking()
            .FirstOrDefaultAsync(c => c.Id == categoryId && c.EventId == eventId);
        
        if (category is null)
        {
            return NotFound(ErrorResponse.FromMessage("Category not found"));
        }
        
        return Ok(MapToResponse(category));
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
}
