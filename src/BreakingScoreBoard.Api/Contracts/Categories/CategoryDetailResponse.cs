using BreakingScoreBoard.Api.Contracts.Registrations;

namespace BreakingScoreBoard.Api.Contracts.Categories;

/// <summary>
/// Detailed category response including registrations.
/// </summary>
public sealed record CategoryDetailResponse
{
    /// <summary>
    /// Unique category ID.
    /// </summary>
    public required Guid Id { get; init; }

    /// <summary>
    /// Category name (e.g., "U14", "U18", "Open").
    /// </summary>
    public required string Name { get; init; }

    /// <summary>
    /// Maximum age for this category (null for open categories).
    /// </summary>
    public int? MaxAge { get; init; }

    /// <summary>
    /// Bracket size (8, 16, 32, or 64).
    /// </summary>
    public required int BracketSize { get; init; }

    /// <summary>
    /// List of registrations in this category.
    /// </summary>
    public required List<RegistrationResponse> Registrations { get; init; }
}
