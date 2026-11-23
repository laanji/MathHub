using MathHub.Modules.Assessment.Domain;
namespace MathHub.Modules.Assessment.Application;
public sealed record ScorePreview(double Estimate, int MathScore, Rank Rank);
public sealed class ScorePreviewService(IScoringEngine engine)
{
    public ScorePreview Calculate(double theta, double difficulty, bool correct)
    {
        var estimate = engine.Update(theta, difficulty, correct, 1);
        var score = MathScore.FromEstimate(estimate);
        return new(estimate, score.Value, RankResolver.Resolve(score));
    }
}
