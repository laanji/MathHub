using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using MathHub.Infrastructure.Persistence;
using MathHub.Modules.Assessment.Application;
using MathHub.Modules.Assessment.Domain;
using MathHub.SharedKernel.Application;
namespace MathHub.Infrastructure.Assessment;
public sealed class AssessmentService(MathHubDbContext db, IScoringEngine engine, IClock clock) : IAssessmentService
{
    private static RequestRejectedException Fail(ErrorKind kind, string code, string message) => new(kind, code, message);
    public async Task<TestView> StartAsync(Guid studentId, string context, CancellationToken ct)
    {
        var info = SeedData.Contexts.SingleOrDefault(c => c.Id == context) ?? throw Fail(ErrorKind.Validation, "test.context", "Оберіть навчальний контекст.");
        await using var tx = await db.Database.BeginTransactionAsync(ct);
        var user = await db.Accounts.FromSqlInterpolated($"SELECT * FROM identity.users WHERE id={studentId} FOR UPDATE").SingleAsync(ct);
        var active = await db.Tests.SingleOrDefaultAsync(t => t.StudentId == studentId && t.Status == "InProgress", ct);
        if (active is not null) return await ViewAsync(active, ct);
        if (user.LastTestAt > clock.UtcNow.AddDays(-30)) throw Fail(ErrorKind.Conflict, "test.retake", "Повторна діагностика доступна через 30 днів після попередньої.");
        var test = new TestRow { StudentId = studentId, Context = context, Theta = info.InitialTheta, StartedAt = clock.UtcNow };
        var question = await NextAsync(test, ct) ?? throw Fail(ErrorKind.Conflict, "test.bank", "Для цього контексту немає завдань.");
        test.CurrentQuestionId = question.Id; db.Tests.Add(test); await db.SaveChangesAsync(ct); await tx.CommitAsync(ct); return await ViewAsync(test, ct);
    }
    public async Task<TestView?> GetCurrentAsync(Guid studentId, CancellationToken ct)
    {
        var test = await db.Tests.AsNoTracking().Where(t => t.StudentId == studentId).OrderByDescending(t => t.StartedAt).FirstOrDefaultAsync(ct);
        return test is null ? null : await ViewAsync(test, ct);
    }
    public async Task<TestView> GetAsync(Guid studentId, Guid testId, CancellationToken ct)
    {
        var test = await db.Tests.AsNoTracking().SingleOrDefaultAsync(t => t.Id == testId && t.StudentId == studentId, ct) ?? throw Fail(ErrorKind.NotFound, "test.missing", "Тест не знайдено.");
        return await ViewAsync(test, ct);
    }
    public async Task<TestView> SubmitAsync(Guid studentId, Guid testId, AnswerCommand command, CancellationToken ct)
    {
        if (command.RequestId == Guid.Empty || command.QuestionId == Guid.Empty || string.IsNullOrWhiteSpace(command.Answer) || command.Answer.Length > 200)
            throw Fail(ErrorKind.Validation, "answer.invalid", "Оберіть відповідь на поточне завдання.");
        await using var tx = await db.Database.BeginTransactionAsync(ct);
        var test = await db.Tests.FromSqlInterpolated($"SELECT * FROM assessment.tests WHERE id={testId} AND student_id={studentId} FOR UPDATE").SingleOrDefaultAsync(ct) ?? throw Fail(ErrorKind.NotFound, "test.missing", "Тест не знайдено.");
        var previous = await db.Answers.SingleOrDefaultAsync(a => a.TestId == testId && a.RequestId == command.RequestId, ct);
        if (previous is not null)
        {
            if (previous.QuestionId != command.QuestionId || previous.RawAnswer != command.Answer.Trim() || previous.Number - 1 != command.Version) throw Fail(ErrorKind.Conflict, "idempotency.conflict", "Цей ключ запиту вже використаний для іншої відповіді.");
            return JsonSerializer.Deserialize<TestView>(previous.ResponseJson)!;
        }
        if (test.Status != "InProgress" || test.Version != command.Version || test.CurrentQuestionId != command.QuestionId)
            throw Fail(ErrorKind.Conflict, "test.version", "Стан тесту змінився. Оновіть поточне завдання.");
        var q = await db.Questions.SingleAsync(q => q.Id == command.QuestionId, ct);
        var options = JsonSerializer.Deserialize<string[]>(q.OptionsJson)!;
        if (!options.Contains(command.Answer.Trim())) throw Fail(ErrorKind.Validation, "answer.option", "Відповідь відсутня серед запропонованих варіантів.");
        var answers = await db.Answers.Where(a => a.TestId == testId).OrderBy(a => a.Number).ToListAsync(ct);
        var domain = TestSession.Restore(test.Id, studentId, test.Theta, TestStatus.InProgress, answers.Select(a => new TestAnswer(a.QuestionId, a.QuestionVersion, a.DifficultySnapshot, a.Correct, a.ThetaAfter, a.RequestId)));
        var answer = domain.Submit(q.Id, q.Version, q.Difficulty, command.Answer.Trim() == q.CorrectAnswer, command.RequestId, test.Version, engine);
        var row = new AnswerRow { TestId = testId, QuestionId = q.Id, RequestId = command.RequestId, QuestionVersion = q.Version, DifficultySnapshot = q.Difficulty, RawAnswer = command.Answer.Trim(), Correct = answer.Correct, ThetaAfter = answer.ThetaAfter, Number = domain.Version };
        db.Answers.Add(row); test.Theta = domain.Theta; test.Version = domain.Version; test.Status = domain.Status.ToString();
        await db.SaveChangesAsync(ct);
        if (domain.Status == TestStatus.InProgress)
        {
            var next = await NextAsync(test, ct);
            if (next is null) { domain.MarkBankExhausted(); test.Status = domain.Status.ToString(); test.CurrentQuestionId = null; } else test.CurrentQuestionId = next.Id;
        }
        if (domain.Status == TestStatus.Completed)
        {
            test.CompletedAt = clock.UtcNow; test.Score = domain.Result!.Value.Value; test.CurrentQuestionId = null;
            var user = await db.Accounts.SingleAsync(u => u.Id == studentId, ct); user.MathScore = test.Score; user.LastTestAt = clock.UtcNow; user.WaitingSince = clock.UtcNow;
        }
        await db.SaveChangesAsync(ct); var view = await ViewAsync(test, ct); row.ResponseJson = JsonSerializer.Serialize(view); await db.SaveChangesAsync(ct); await tx.CommitAsync(ct); return view;
    }
    private async Task<QuestionRow?> NextAsync(TestRow test, CancellationToken ct)
    {
        var excluded = await db.Answers.Where(a => a.TestId == test.Id).Select(a => a.QuestionId).ToListAsync(ct);
        return await db.Questions.Where(q => q.Context == test.Context && q.Active && !excluded.Contains(q.Id)).OrderBy(q => Math.Abs(q.Difficulty - test.Theta)).ThenBy(q => q.Id).FirstOrDefaultAsync(ct);
    }
    private async Task<TestView> ViewAsync(TestRow test, CancellationToken ct)
    {
        QuestionView? question = null; ResultView? result = null;
        if (test.Status == "InProgress" && test.CurrentQuestionId is Guid questionId)
        { var q = await db.Questions.AsNoTracking().SingleAsync(q => q.Id == questionId, ct); question = new(q.Id, q.Text, q.Topic, JsonSerializer.Deserialize<string[]>(q.OptionsJson)!); }
        if (test.Status == "Completed" && test.Score is int score)
        { var rank = RankResolver.Resolve(new(score)); var correct = await db.Answers.CountAsync(a => a.TestId == test.Id && a.Correct, ct); result = new(score, rank.Name, rank.SubRank, correct, test.Version, test.CompletedAt!.Value); }
        return new(test.Id, test.Context, test.Status, test.Version, 15, 20, question, result);
    }
}
