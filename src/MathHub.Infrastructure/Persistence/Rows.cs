namespace MathHub.Infrastructure.Persistence;
public sealed class AccountRow
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Email { get; set; } = "";
    public string DisplayName { get; set; } = "";
    public string PasswordHash { get; set; } = "";
    public string Role { get; set; } = "Student";
    public DateOnly BirthDate { get; set; }
    public bool IsActive { get; set; }
    public int FailedLogins { get; set; }
    public DateTimeOffset? LockedUntil { get; set; }
    public int? MathScore { get; set; }
    public DateTimeOffset? LastTestAt { get; set; }
    public DateTimeOffset? WaitingSince { get; set; }
}
public sealed class ParentLinkRow
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid ChildId { get; set; }
    public string ParentEmail { get; set; } = "";
    public Guid? ParentId { get; set; }
    public string CodeHash { get; set; } = "";
    public int FailedAttempts { get; set; }
    public DateTimeOffset ExpiresAt { get; set; }
    public DateTimeOffset? ConfirmedAt { get; set; }
    public string ConsentVersion { get; set; } = "2026-11-v1";
}
public sealed class AuthSessionRow
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid UserId { get; set; }
    public DateTimeOffset ExpiresAt { get; set; }
    public bool Revoked { get; set; }
}
public sealed class RefreshTokenRow
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid SessionId { get; set; }
    public string TokenHash { get; set; } = "";
    public DateTimeOffset ExpiresAt { get; set; }
    public bool Used { get; set; }
}
public sealed class QuestionRow
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Context { get; set; } = "";
    public string Text { get; set; } = "";
    public string Topic { get; set; } = "";
    public string OptionsJson { get; set; } = "[]";
    public string CorrectAnswer { get; set; } = "";
    public double Difficulty { get; set; }
    public int Version { get; set; } = 1;
    public bool Active { get; set; } = true;
}
public sealed class TestRow
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid StudentId { get; set; }
    public string Context { get; set; } = "";
    public string Status { get; set; } = "InProgress";
    public double Theta { get; set; }
    public int Version { get; set; }
    public Guid? CurrentQuestionId { get; set; }
    public DateTimeOffset StartedAt { get; set; }
    public DateTimeOffset? CompletedAt { get; set; }
    public int? Score { get; set; }
}
public sealed class AnswerRow
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid TestId { get; set; }
    public Guid QuestionId { get; set; }
    public Guid RequestId { get; set; }
    public int QuestionVersion { get; set; }
    public string RawAnswer { get; set; } = "";
    public double DifficultySnapshot { get; set; }
    public bool Correct { get; set; }
    public double ThetaAfter { get; set; }
    public int Number { get; set; }
    public string ResponseJson { get; set; } = "{}";
}
public sealed class TeacherRow
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string DisplayName { get; set; } = "";
    public string Subject { get; set; } = "";
    public string Bio { get; set; } = "";
    public string PhotoPath { get; set; } = "";
    public bool IsPlaceholder { get; set; } = true;
}
public sealed class DemoCheckoutRow
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid UserId { get; set; }
    public Guid CourseId { get; set; }
    public Guid RequestId { get; set; }
    public string CourseTitle { get; set; } = "";
    public decimal Amount { get; set; }
    public string Scenario { get; set; } = "success";
    public string Status { get; set; } = "Succeeded";
    public string GatewayReference { get; set; } = "";
    public DateTimeOffset CreatedAt { get; set; }
}
public sealed class CourseRow
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Title { get; set; } = "";
    public string Description { get; set; } = "";
    public string Context { get; set; } = "";
    public decimal PricePerLesson { get; set; }
    public string SlotsJson { get; set; } = "[]";
    public bool Active { get; set; } = true;
}
public sealed class GroupRow
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid CourseId { get; set; }
    public string AgeCohort { get; set; } = "";
    public string Slot { get; set; } = "";
    public int DeltaLimit { get; set; } = 5;
    public bool Archived { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
}
public sealed class EnrollmentRow
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid StudentId { get; set; }
    public Guid CourseId { get; set; }
    public Guid GroupId { get; set; }
    public int ScoreAtEntry { get; set; }
    public decimal PriceSnapshot { get; set; }
    public string Status { get; set; } = "Reserved";
    public DateTimeOffset ReservedUntil { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
}
public sealed class PaymentRow
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid StudentId { get; set; }
    public Guid EnrollmentId { get; set; }
    public Guid RequestId { get; set; }
    public decimal Amount { get; set; }
    public string Status { get; set; } = "Succeeded";
    public string GatewayReference { get; set; } = "";
    public DateTimeOffset CreatedAt { get; set; }
}
public sealed class ReceiptRow
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid StudentId { get; set; }
    public Guid RequestId { get; set; }
    public string Scope { get; set; } = "";
    public string PayloadHash { get; set; } = "";
    public string ResponseJson { get; set; } = "{}";
}
