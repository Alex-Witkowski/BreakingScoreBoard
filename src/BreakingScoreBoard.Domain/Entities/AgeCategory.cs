using BreakingScoreBoard.Domain.Enums;

namespace BreakingScoreBoard.Domain.Entities;

/// <summary>
/// Represents an age bracket within a battle event.
/// </summary>
public class AgeCategory
{
    public Guid Id { get; set; }
    
    /// <summary>Parent event ID.</summary>
    public Guid EventId { get; set; }
    
    /// <summary>Display name (e.g., "U14", "Open").</summary>
    public string Name { get; set; } = string.Empty;
    
    /// <summary>Upper age limit (null = no limit, e.g., "Open").</summary>
    public int? MaxAge { get; set; }
    
    /// <summary>Target bracket size (8, 16, 32, or 64).</summary>
    public int BracketSize { get; set; }
    
    /// <summary>Current phase of this category.</summary>
    public CategoryPhase CurrentPhase { get; set; } = CategoryPhase.Registration;
    
    /// <summary>Display order within the event.</summary>
    public int SortOrder { get; set; }
    
    // Navigation properties
    public BattleEvent Event { get; set; } = null!;
    public ICollection<Registration> Registrations { get; set; } = new List<Registration>();
    public ICollection<Battle> Battles { get; set; } = new List<Battle>();
    
    /// <summary>
    /// Validates that MaxAge is positive if specified.
    /// </summary>
    public bool IsValidMaxAge() => MaxAge is null or > 0;
    
    /// <summary>
    /// Validates that BracketSize is a valid power of 2 (8, 16, 32, or 64).
    /// </summary>
    public bool IsValidBracketSize() => BracketSize is 8 or 16 or 32 or 64;
}
