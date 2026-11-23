using MathHub.SharedKernel.Domain;
namespace MathHub.Modules.Assessment.Domain;
public sealed class EloScoringEngine : IScoringEngine
{
    public double Update(double theta, double difficulty, bool correct, int answerNumber)
    {
        if (!double.IsFinite(theta) || !double.IsFinite(difficulty) || theta is < 0 or > 100 || difficulty is < 0 or > 100 || answerNumber < 1)
            throw new BusinessRuleException("assessment.arguments", "Некоректні параметри відповіді.");
        var expected = 1 / (1 + Math.Pow(10, (difficulty - theta) / 20));
        var k = answerNumber <= 5 ? 8 : answerNumber <= 10 ? 4 : 2;
        return Math.Clamp(theta + k * ((correct ? 1 : 0) - expected), 0, 100);
    }
}
