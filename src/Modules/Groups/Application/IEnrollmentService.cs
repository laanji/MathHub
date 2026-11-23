namespace MathHub.Modules.Groups.Application;
public sealed record CourseView(Guid Id, string Title, string Description, string Context, decimal PricePerLesson, string[] Slots);
public sealed record ReserveCommand(Guid CourseId, string Slot, Guid RequestId);
public sealed record EnrollmentView(Guid Id, Guid CourseId, string CourseTitle, Guid GroupId, string Slot, string Status, string GroupState, int ActiveCount, int OccupiedSeats, int ScoreAtEntry, decimal PriceSnapshot, DateTimeOffset ReservedUntil);
public sealed record PaymentConfirmation(bool Confirmed, EnrollmentView? Enrollment, string Message);
public interface IEnrollmentService
{
    Task<IReadOnlyList<CourseView>> GetCoursesAsync(CancellationToken ct);
    Task<IReadOnlyList<EnrollmentView>> GetMineAsync(Guid studentId, CancellationToken ct);
    Task<EnrollmentView> ReserveAsync(Guid studentId, ReserveCommand command, CancellationToken ct);
    Task<PaymentConfirmation> ConfirmAsync(Guid studentId, Guid enrollmentId, Guid requestId, CancellationToken ct);
    Task CancelAsync(Guid studentId, Guid enrollmentId, CancellationToken ct);
}
