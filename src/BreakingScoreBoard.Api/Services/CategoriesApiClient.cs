using BreakingScoreBoard.Api.Contracts.Categories;
using BreakingScoreBoard.Api.Contracts.Events;

namespace BreakingScoreBoard.Api.Services;

/// <summary>
/// API client for Categories endpoints
/// </summary>
public class CategoriesApiClient : ApiClientBase
{
    public CategoriesApiClient(HttpClient httpClient, ILogger<CategoriesApiClient> logger)
        : base(httpClient, logger)
    {
    }

    /// <summary>
    /// Get category by ID
    /// </summary>
    public async Task<CategoryDetailResponse> GetCategoryByIdAsync(Guid categoryId, CancellationToken ct = default)
    {
        return await GetAsync<CategoryDetailResponse>($"/categories/{categoryId}", ct);
    }

    /// <summary>
    /// Create a new category for an event
    /// </summary>
    public async Task<CategoryResponse> CreateCategoryAsync(
        Guid eventId,
        CreateCategoryRequest request,
        string adminPin,
        CancellationToken ct = default)
    {
        return await PostAsync<CreateCategoryRequest, CategoryResponse>(
            $"/events/{eventId}/categories",
            request,
            adminPin,
            ct);
    }

    /// <summary>
    /// Start pre-selection for a category
    /// </summary>
    public async Task StartPreSelectionAsync(
        Guid eventId,
        Guid categoryId,
        string adminPin,
        CancellationToken ct = default)
    {
        await PostAsync<object>(
            $"/events/{eventId}/categories/{categoryId}/start-preselection",
            new { },
            adminPin,
            ct);
    }

    /// <summary>
    /// Start initial bracket for a category
    /// </summary>
    public async Task StartBracketAsync(
        Guid eventId,
        Guid categoryId,
        string adminPin,
        CancellationToken ct = default)
    {
        await PostAsync<object>(
            $"/events/{eventId}/categories/{categoryId}/start-bracket",
            new { },
            adminPin,
            ct);
    }

    /// <summary>
    /// Advance bracket to next level
    /// </summary>
    public async Task AdvanceBracketAsync(
        Guid eventId,
        Guid categoryId,
        string adminPin,
        CancellationToken ct = default)
    {
        await PostAsync<object>(
            $"/events/{eventId}/categories/{categoryId}/advance-bracket",
            new { },
            adminPin,
            ct);
    }

    /// <summary>
    /// Reset category by deleting all battles and returning to registration phase
    /// </summary>
    public async Task ResetCategoryAsync(
        Guid eventId,
        Guid categoryId,
        string adminPin,
        CancellationToken ct = default)
    {
        await PostAsync<object>(
            $"/events/{eventId}/categories/{categoryId}/reset",
            new { },
            adminPin,
            ct);
    }

    /// <summary>
    /// Delete a category (only if it has no registrations)
    /// </summary>
    public async Task DeleteCategoryAsync(
        Guid eventId,
        Guid categoryId,
        string adminPin,
        CancellationToken ct = default)
    {
        await DeleteAsync(
            $"/events/{eventId}/categories/{categoryId}",
            adminPin,
            ct);
    }
}
