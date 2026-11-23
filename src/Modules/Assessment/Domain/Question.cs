using MathHub.SharedKernel.Domain;

namespace MathHub.Modules.Assessment.Domain;

public sealed class Question
{
    public Guid Id { get; } = Guid.NewGuid();
    public int Version { get; }
    public string Topic { get; }
    public string Text { get; }
    public double Difficulty { get; }
    public bool IsActive { get; private set; } = true;
    private readonly string _expectedAnswer;

    public Question(string text, string topic, double difficulty, string expectedAnswer, int version = 1)
    {
        if (string.IsNullOrWhiteSpace(text) || string.IsNullOrWhiteSpace(topic)
            || string.IsNullOrWhiteSpace(expectedAnswer) || version < 1
            || !double.IsFinite(difficulty) || difficulty is < 0 or > 100)
            throw new BusinessRuleException("question.invalid", "Заповніть текст, тему, відповідь і складність питання.");

        Text = text.Trim();
        Topic = topic.Trim();
        Difficulty = difficulty;
        _expectedAnswer = expectedAnswer.Trim();
        Version = version;
    }

    // MVP supports exact short answers. Numeric tolerances and symbolic expressions need separate strategies.
    public bool Evaluate(string answer) => string.Equals(_expectedAnswer, answer?.Trim(), StringComparison.OrdinalIgnoreCase);
    public void Archive() => IsActive = false;
}
