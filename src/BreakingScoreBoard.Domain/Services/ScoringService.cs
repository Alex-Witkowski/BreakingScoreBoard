using BreakingScoreBoard.Domain.Entities;

namespace BreakingScoreBoard.Domain.Services;

/// <summary>
/// Service for calculating battle scores and determining winners.
/// </summary>
public class ScoringService
{
    /// <summary>
    /// Calculates the average score for a breaker from judge scores.
    /// </summary>
    /// <param name="scores">List of judge scores for the breaker.</param>
    /// <returns>Average score, or null if no scores.</returns>
    public decimal? CalculateAverageScore(IEnumerable<JudgeScore> scores)
    {
        var scoreList = scores.ToList();
        if (!scoreList.Any())
        {
            return null;
        }

        return (decimal)scoreList.Average(s => s.Score);
    }

    /// <summary>
    /// Determines the winner of a battle based on judge scores.
    /// </summary>
    /// <param name="battle">The battle entity.</param>
    /// <param name="allScores">All judge scores for the battle.</param>
    /// <returns>Winner breaker ID, or null if tie.</returns>
    public Guid? DetermineWinner(Battle battle, IEnumerable<JudgeScore> allScores)
    {
        var scoresList = allScores.ToList();

        var breaker1Scores = scoresList.Where(s => s.BreakerId == battle.Breaker1Id);
        var breaker2Scores = scoresList.Where(s => s.BreakerId == battle.Breaker2Id);

        var breaker1Avg = CalculateAverageScore(breaker1Scores);
        var breaker2Avg = CalculateAverageScore(breaker2Scores);

        if (!breaker1Avg.HasValue || !breaker2Avg.HasValue)
        {
            return null; // Incomplete scoring
        }

        if (breaker1Avg.Value > breaker2Avg.Value)
        {
            return battle.Breaker1Id;
        }
        else if (breaker2Avg.Value > breaker1Avg.Value)
        {
            return battle.Breaker2Id;
        }
        else
        {
            return null; // Tie
        }
    }

    /// <summary>
    /// Checks if a battle is a tie (equal average scores).
    /// </summary>
    /// <param name="battle">The battle entity.</param>
    /// <param name="allScores">All judge scores for the battle.</param>
    /// <returns>True if the battle is a tie.</returns>
    public bool IsTie(Battle battle, IEnumerable<JudgeScore> allScores)
    {
        var scoresList = allScores.ToList();

        var breaker1Scores = scoresList.Where(s => s.BreakerId == battle.Breaker1Id);
        var breaker2Scores = scoresList.Where(s => s.BreakerId == battle.Breaker2Id);

        var breaker1Avg = CalculateAverageScore(breaker1Scores);
        var breaker2Avg = CalculateAverageScore(breaker2Scores);

        if (!breaker1Avg.HasValue || !breaker2Avg.HasValue)
        {
            return false; // Incomplete scoring is not a tie
        }

        return breaker1Avg.Value == breaker2Avg.Value;
    }

    /// <summary>
    /// Validates that scores in a re-battle are differentiated (no judge can give same score to both breakers).
    /// </summary>
    /// <param name="allScores">All judge scores for the re-battle.</param>
    /// <param name="breaker1Id">First breaker ID.</param>
    /// <param name="breaker2Id">Second breaker ID.</param>
    /// <returns>True if all judges differentiated their scores.</returns>
    public bool ValidateForcedDifferentiation(IEnumerable<JudgeScore> allScores, Guid breaker1Id, Guid breaker2Id)
    {
        var scoresList = allScores.ToList();

        // Group by judge identifier
        var judgeGroups = scoresList.GroupBy(s => s.JudgeIdentifier);

        foreach (var judgeGroup in judgeGroups)
        {
            var breaker1Score = judgeGroup.FirstOrDefault(s => s.BreakerId == breaker1Id)?.Score;
            var breaker2Score = judgeGroup.FirstOrDefault(s => s.BreakerId == breaker2Id)?.Score;

            // If both scores are present and they are equal, differentiation failed
            if (breaker1Score.HasValue && breaker2Score.HasValue && breaker1Score.Value == breaker2Score.Value)
            {
                return false;
            }
        }

        return true;
    }
}
