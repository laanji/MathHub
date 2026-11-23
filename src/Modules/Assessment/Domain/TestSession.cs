using MathHub.SharedKernel.Domain;
namespace MathHub.Modules.Assessment.Domain;
public enum TestStatus { InProgress, Completed, InsufficientData }
public sealed record TestAnswer(Guid QuestionId, int QuestionVersion, double DifficultySnapshot, bool Correct, double ThetaAfter, Guid RequestId);
public sealed class TestSession
{
    private readonly List<TestAnswer> _answers = [];
    public Guid Id { get; private set; } = Guid.NewGuid();
    public Guid StudentId { get; }
    public double Theta { get; private set; }
    public TestStatus Status { get; private set; } = TestStatus.InProgress;
    public int Version => _answers.Count;
    public IReadOnlyList<TestAnswer> Answers => _answers.AsReadOnly();
    public MathScore? Result => Status == TestStatus.Completed ? MathScore.FromEstimate(Theta) : null;
    public TestSession(Guid studentId, double initialTheta)
    {
        if (studentId == Guid.Empty || !double.IsFinite(initialTheta) || initialTheta is < 0 or > 100)
            throw new BusinessRuleException("assessment.session", "Некоректні параметри сесії.");
        StudentId = studentId; Theta = initialTheta;
    }
    public static TestSession Restore(Guid id, Guid studentId, double theta, TestStatus status, IEnumerable<TestAnswer> answers)
    {
        var session = new TestSession(studentId, theta) { Id = id, Status = status };
        session._answers.AddRange(answers);
        return session;
    }
    public TestAnswer Submit(Guid questionId, int questionVersion, double difficulty, bool correct, Guid requestId, int expectedVersion, IScoringEngine engine)
    {
        var previous = _answers.SingleOrDefault(a => a.RequestId == requestId);
        if (previous is not null)
        {
            if (previous.QuestionId != questionId || previous.Correct != correct || previous.QuestionVersion != questionVersion || previous.DifficultySnapshot != difficulty)
                throw new BusinessRuleException("idempotency.conflict", "Ключ запиту вже використаний для інших даних.");
            return previous;
        }
        if (requestId == Guid.Empty || questionId == Guid.Empty || questionVersion < 1)
            throw new BusinessRuleException("assessment.answer", "Некоректна відповідь.");
        if (Status != TestStatus.InProgress || expectedVersion != Version)
            throw new BusinessRuleException("assessment.version", "Сесію завершено або її версія змінилася.");
        if (_answers.Any(a => a.QuestionId == questionId))
            throw new BusinessRuleException("assessment.duplicate", "Це питання вже було показане.");
        Theta = engine.Update(Theta, difficulty, correct, Version + 1);
        var answer = new TestAnswer(questionId, questionVersion, difficulty, correct, Theta, requestId);
        _answers.Add(answer);
        var window = _answers.TakeLast(5).Select(a => a.ThetaAfter).ToArray();
        if (Version >= 20 || Version >= 15 && window.Max() - window.Min() < 1) Status = TestStatus.Completed;
        return answer;
    }
    public void MarkBankExhausted()
    {
        if (Status == TestStatus.InProgress) Status = Version < 15 ? TestStatus.InsufficientData : TestStatus.Completed;
    }
}
