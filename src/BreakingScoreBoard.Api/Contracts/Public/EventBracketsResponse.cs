namespace BreakingScoreBoard.Api.Contracts.Public;

/// <summary>
/// Tournament brackets for all categories in an event.
/// </summary>
public sealed record EventBracketsResponse
{
    /// <summary>
    /// Event ID.
    /// </summary>
    public required Guid EventId { get; init; }

    /// <summary>
    /// Event title.
    /// </summary>
    public required string EventTitle { get; init; }

    /// <summary>
    /// Brackets for each category.
    /// </summary>
    public required List<CategoryBracketResponse> Categories { get; init; }
}
