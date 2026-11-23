using MathHub.Modules.Assessment.Domain;

namespace MathHub.Modules.Assessment.Application;

public sealed record QuestionSelection(string Context, double TargetDifficulty, IReadOnlySet<Guid> ExcludedIds);

public interface IQuestionBank
{
    Task<Question?> SelectNextAsync(QuestionSelection selection, CancellationToken cancellationToken);
}
