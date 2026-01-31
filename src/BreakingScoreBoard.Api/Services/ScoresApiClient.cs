using BreakingScoreBoard.Api.Contracts.Scores;

namespace BreakingScoreBoard.Api.Services;

/// <summary>
/// API client for Scores endpoints
/// </summary>
public class ScoresApiClient : ApiClientBase
{
    public ScoresApiClient(HttpClient httpClient, ILogger<ScoresApiClient> logger)
        : base(httpClient, logger)
    {
    }

    /// <summary>
    /// Submit scores for a battle
    /// </summary>
    public async Task SubmitScoresAsync(
        SubmitScoresRequest request,
        string judgePin,
        CancellationToken ct = default)
    {
        await PostAsync<SubmitScoresRequest>(
            "/scores",
            request,
            judgePin,
            ct);
    }

    /// <summary>
    /// Get all scores for a battle
    /// </summary>
    public async Task<List<JudgeScoreResponse>> GetScoresForBattleAsync(
        Guid battleId,
        CancellationToken ct = default)
    {
        return await GetAsync<List<JudgeScoreResponse>>($"/battles/{battleId}/scores", ct);
    }
}
