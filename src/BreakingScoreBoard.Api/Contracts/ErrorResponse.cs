namespace BreakingScoreBoard.Api.Contracts;

/// <summary>
/// Standard error response format.
/// </summary>
public record ErrorResponse
{
    /// <summary>
    /// Error message.
    /// </summary>
    public required string Error { get; init; }
    
    /// <summary>
    /// Optional detailed error messages.
    /// </summary>
    public IReadOnlyList<string>? Details { get; init; }
    
    /// <summary>
    /// Creates an error response with a single message.
    /// </summary>
    public static ErrorResponse FromMessage(string message) => new() { Error = message };
    
    /// <summary>
    /// Creates an error response with details.
    /// </summary>
    public static ErrorResponse FromDetails(string message, IEnumerable<string> details) => 
        new() { Error = message, Details = details.ToList() };
}
