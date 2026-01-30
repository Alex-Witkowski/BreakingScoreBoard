using BreakingScoreBoard.Domain.Enums;

namespace BreakingScoreBoard.Domain.Entities;

/// <summary>
/// Links a breaker to an age category within an event.
/// </summary>
public class Registration
{
    public Guid Id { get; set; }
    
    /// <summary>The participant.</summary>
    public Guid BreakerId { get; set; }
    
    /// <summary>The age category.</summary>
    public Guid CategoryId { get; set; }
    
    /// <summary>UTC timestamp when registered.</summary>
    public DateTime RegisteredAt { get; set; }
    
    /// <summary>Seeding rank (if pre-seeded).</summary>
    public int? Seed { get; set; }
    
    /// <summary>Registration status.</summary>
    public RegistrationStatus Status { get; set; } = RegistrationStatus.Active;
    
    // Navigation properties
    public Breaker Breaker { get; set; } = null!;
    public AgeCategory Category { get; set; } = null!;
}
