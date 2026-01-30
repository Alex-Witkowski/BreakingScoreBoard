using BreakingScoreBoard.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace BreakingScoreBoard.Api.Infrastructure;

/// <summary>
/// Service for PIN-based authentication.
/// </summary>
public class PinAuthService
{
    private readonly BattleDbContext _dbContext;
    private readonly IConfiguration _configuration;
    
    public PinAuthService(BattleDbContext dbContext, IConfiguration configuration)
    {
        _dbContext = dbContext;
        _configuration = configuration;
    }
    
    /// <summary>
    /// Validates the admin PIN from configuration.
    /// </summary>
    /// <param name="pin">The PIN to validate.</param>
    /// <returns>True if the PIN is valid.</returns>
    public bool ValidateGlobalAdminPin(string pin)
    {
        var adminPin = _configuration["AdminPin"];
        return !string.IsNullOrEmpty(adminPin) && adminPin == pin;
    }
    
    /// <summary>
    /// Validates the admin PIN for a specific event.
    /// </summary>
    /// <param name="eventId">The event ID.</param>
    /// <param name="pin">The PIN to validate.</param>
    /// <returns>True if the PIN is valid.</returns>
    public async Task<bool> ValidateEventAdminPinAsync(Guid eventId, string pin)
    {
        var battleEvent = await _dbContext.BattleEvents
            .AsNoTracking()
            .FirstOrDefaultAsync(e => e.Id == eventId);
        
        if (battleEvent is null)
        {
            return false;
        }
        
        return PinHasher.Verify(pin, battleEvent.AdminPinHash);
    }
    
    /// <summary>
    /// Validates the judge PIN for a specific event.
    /// </summary>
    /// <param name="eventId">The event ID.</param>
    /// <param name="pin">The PIN to validate.</param>
    /// <returns>True if the PIN is valid.</returns>
    public async Task<bool> ValidateEventJudgePinAsync(Guid eventId, string pin)
    {
        var battleEvent = await _dbContext.BattleEvents
            .AsNoTracking()
            .FirstOrDefaultAsync(e => e.Id == eventId);
        
        if (battleEvent is null)
        {
            return false;
        }
        
        return PinHasher.Verify(pin, battleEvent.JudgePinHash);
    }
    
    /// <summary>
    /// Validates either admin or judge PIN for a specific event.
    /// </summary>
    /// <param name="eventId">The event ID.</param>
    /// <param name="pin">The PIN to validate.</param>
    /// <returns>True if the PIN matches either admin or judge PIN.</returns>
    public async Task<bool> ValidateEventPinAsync(Guid eventId, string pin)
    {
        var battleEvent = await _dbContext.BattleEvents
            .AsNoTracking()
            .FirstOrDefaultAsync(e => e.Id == eventId);
        
        if (battleEvent is null)
        {
            return false;
        }
        
        return PinHasher.Verify(pin, battleEvent.AdminPinHash) || 
               PinHasher.Verify(pin, battleEvent.JudgePinHash);
    }
}
