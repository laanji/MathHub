namespace MathHub.Modules.Assessment.Application;
public sealed record ContextView(string Id, string Name, double InitialTheta);
public sealed record QuestionView(Guid Id, string Text, string Topic, string[] Options);
public sealed record AnswerCommand(Guid QuestionId, string Answer, Guid RequestId, int Version);
public sealed record ResultView(int MathScore, string Rank, string SubRank, int Correct, int Total, DateTimeOffset CompletedAt);
public sealed record TestView(Guid Id, string Context, string Status, int Version, int MinimumQuestions, int MaximumQuestions, QuestionView? Question, ResultView? Result);
public interface IAssessmentService
{
    Task<TestView> StartAsync(Guid studentId, string context, CancellationToken ct);
    Task<TestView?> GetCurrentAsync(Guid studentId, CancellationToken ct);
    Task<TestView> GetAsync(Guid studentId, Guid testId, CancellationToken ct);
    Task<TestView> SubmitAsync(Guid studentId, Guid testId, AnswerCommand command, CancellationToken ct);
}
