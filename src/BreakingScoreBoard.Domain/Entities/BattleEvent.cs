namespace BreakingScoreBoard.Domain.Entities;

/// <summary>
/// Represents a competition event (aggregate root).
/// </summary>
public class BattleEvent
{
    public Guid Id { get; set; }
    
    /// <summary>Event name (e.g., "Berlin Breaking Championship 2026").</summary>
    public string Title { get; set; } = string.Empty;
    
    /// <summary>Date of the competition.</summary>
    public DateOnly EventDate { get; set; }
    
    /// <summary>Venue name/address (optional).</summary>
    public string? Location { get; set; }
    
    /// <summary>Number of judges per battle (3 or 5).</summary>
    public int JudgeCount { get; set; }
    
    /// <summary>SHA256 hash of organizer PIN.</summary>
    public string AdminPinHash { get; set; } = string.Empty;
    
    /// <summary>SHA256 hash of judge access PIN.</summary>
    public string JudgePinHash { get; set; } = string.Empty;
    
    /// <summary>Whether new registrations are accepted.</summary>
    public bool RegistrationOpen { get; set; } = true;
    
    /// <summary>UTC timestamp when created.</summary>
    public DateTime CreatedAt { get; set; }
    
    /// <summary>UTC timestamp when last updated.</summary>
    public DateTime UpdatedAt { get; set; }
    
    // Navigation properties
    public ICollection<AgeCategory> Categories { get; set; } = new List<AgeCategory>();
    
    /// <summary>
    /// Validates the judge count is either 3 or 5.
    /// </summary>
    public bool IsValidJudgeCount() => JudgeCount == 3 || JudgeCount == 5;
    
    /// <summary>
    /// Validates that admin and judge PINs are different.
    /// </summary>
    public bool HasDistinctPins() => AdminPinHash != JudgePinHash;
}
