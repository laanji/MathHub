using MathHub.SharedKernel.Domain;
namespace MathHub.Modules.Assessment.Domain;
public readonly record struct MathScore
{
    public int Value { get; }
    public MathScore(int value)
    {
        if (value is < 0 or > 100) throw new BusinessRuleException("score.range", "MathScore має бути від 0 до 100.");
        Value = value;
    }
    public static MathScore FromEstimate(double theta) => double.IsFinite(theta)
        ? new((int)Math.Round(Math.Clamp(theta, 0, 100), MidpointRounding.AwayFromZero))
        : throw new BusinessRuleException("score.invalid", "Некоректна оцінка знань.");
}
