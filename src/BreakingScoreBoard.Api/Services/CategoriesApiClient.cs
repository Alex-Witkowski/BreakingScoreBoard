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
        Guid categoryId,
        string adminPin,
        CancellationToken ct = default)
    {
        await PostAsync<object>(
            $"/categories/{categoryId}/start-preselection",
            new { },
            adminPin,
            ct);
    }

    /// <summary>
    /// Start bracket for a category
    /// </summary>
    public async Task StartBracketAsync(
        Guid categoryId,
        string adminPin,
        CancellationToken ct = default)
    {
        await PostAsync<object>(
            $"/categories/{categoryId}/start-bracket",
            new { },
            adminPin,
            ct);
    }

    /// <summary>
    /// Advance bracket to next level
    /// </summary>
    public async Task AdvanceBracketAsync(
        Guid categoryId,
        string adminPin,
        CancellationToken ct = default)
    {
        await PostAsync<object>(
            $"/categories/{categoryId}/advance-bracket",
            new { },
            adminPin,
            ct);
    }
}
