using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using MathHub.Infrastructure.Persistence;
using MathHub.Infrastructure.Security;
using MathHub.Modules.Billing.Application;
using MathHub.Modules.Groups.Application;
using MathHub.SharedKernel.Application;
namespace MathHub.Infrastructure.Groups;
public sealed class EnrollmentService(MathHubDbContext db, EfGroupRepository repository, IGroupMatchingStrategy matcher, IPaymentGateway gateway, IClock clock) : IEnrollmentService
{
    private static RequestRejectedException Fail(ErrorKind kind, string code, string message) => new(kind, code, message);
    public async Task<IReadOnlyList<CourseView>> GetCoursesAsync(CancellationToken ct)
    { var rows = await db.Courses.AsNoTracking().Where(c => c.Active).OrderBy(c => c.PricePerLesson).ToListAsync(ct); return rows.Select(c => new CourseView(c.Id, c.Title, c.Description, c.Context, c.PricePerLesson, JsonSerializer.Deserialize<string[]>(c.SlotsJson)!)).ToList(); }
    public async Task<IReadOnlyList<EnrollmentView>> GetMineAsync(Guid studentId, CancellationToken ct)
    {
        var items = await db.Enrollments.AsNoTracking().Where(e => e.StudentId == studentId).OrderByDescending(e => e.CreatedAt).ToListAsync(ct); var views = new List<EnrollmentView>();
        foreach (var e in items) views.Add(await ViewAsync(e, ct)); return views;
    }
    private async Task<AccountRow> LockStudentAsync(Guid id, CancellationToken ct) => await db.Accounts.FromSqlInterpolated($"SELECT * FROM identity.users WHERE id={id} FOR UPDATE").SingleAsync(ct);
    private async Task<CourseRow> LockCourseAsync(Guid id, CancellationToken ct) => await db.Courses.FromSqlInterpolated($"SELECT * FROM groups.courses WHERE id={id} FOR UPDATE").SingleOrDefaultAsync(ct) ?? throw Fail(ErrorKind.NotFound, "course.missing", "Курс не знайдено.");
    private async Task<T?> ReceiptAsync<T>(Guid studentId, string scope, Guid requestId, string payload, CancellationToken ct) where T : class
    {
        var row = await db.Receipts.SingleOrDefaultAsync(r => r.StudentId == studentId && r.Scope == scope && r.RequestId == requestId, ct);
        if (row is null) return null;
        if (row.PayloadHash != IdentityService.Hash(payload)) throw Fail(ErrorKind.Conflict, "idempotency.conflict", "Цей ключ запиту вже використаний для інших даних.");
        return JsonSerializer.Deserialize<T>(row.ResponseJson)!;
    }
    private void Remember<T>(Guid studentId, string scope, Guid requestId, string payload, T result) => db.Receipts.Add(new ReceiptRow { StudentId = studentId, Scope = scope, RequestId = requestId, PayloadHash = IdentityService.Hash(payload), ResponseJson = JsonSerializer.Serialize(result) });
    public async Task<EnrollmentView> ReserveAsync(Guid studentId, ReserveCommand command, CancellationToken ct)
    {
        if (command.RequestId == Guid.Empty) throw Fail(ErrorKind.Validation, "request.id", "Потрібен ідентифікатор запиту.");
        var payload = $"{command.CourseId}|{command.Slot}"; await using var tx = await db.Database.BeginTransactionAsync(ct);
        var user = await LockStudentAsync(studentId, ct);
        var previous = await ReceiptAsync<EnrollmentView>(studentId, "reserve", command.RequestId, payload, ct); if (previous is not null) return previous;
        if (user.MathScore is not int score) throw Fail(ErrorKind.Conflict, "score.required", "Спочатку завершіть діагностику.");
        var course = await LockCourseAsync(command.CourseId, ct);
        if (!course.Active || !JsonSerializer.Deserialize<string[]>(course.SlotsJson)!.Contains(command.Slot)) throw Fail(ErrorKind.Validation, "course.slot", "Оберіть доступний розклад курсу.");
        // Same student lock is used by every enrollment mutation, including expiry and confirmation.
        await db.Enrollments.Where(e => e.StudentId == studentId && e.Status == "Reserved" && e.ReservedUntil <= clock.UtcNow).ExecuteUpdateAsync(e => e.SetProperty(x => x.Status, "Expired"), ct);
        if (await repository.HasActiveCourseEnrollmentAsync(studentId, course.Id, ct)) throw Fail(ErrorKind.Conflict, "course.duplicate", "У вас уже є активний запис або резерв на цей курс.");
        var age = IdentityService.Age(user.BirthDate, clock.UtcNow); var cohort = age < 9 ? "5–8" : age < 13 ? "9–12" : age < 16 ? "13–15" : age < 18 ? "16–17" : "18+";
        var candidates = await db.Groups.Where(g => g.CourseId == course.Id && g.AgeCohort == cohort && g.Slot == command.Slot && !g.Archived).ToListAsync(ct);
        var domains = new List<MathHub.Modules.Groups.Domain.CohortGroup>(); foreach (var row in candidates) domains.Add(await repository.LoadAsync(row, ct));
        var selected = matcher.Select(domains, new(course.Id, cohort, score), clock.UtcNow);
        if (selected is null)
        {
            var row = new GroupRow { CourseId = course.Id, AgeCohort = cohort, Slot = command.Slot, CreatedAt = clock.UtcNow }; db.Groups.Add(row); await db.SaveChangesAsync(ct);
            selected = await repository.GetForUpdateAsync(row.Id, ct);
        }
        else selected = await repository.GetForUpdateAsync(selected.Id, ct);
        selected!.ExpireReservations(clock.UtcNow);
        var enrollment = selected.Reserve(studentId, score, course.PricePerLesson, clock.UtcNow);
        await repository.SaveAsync(selected, ct);
        var stored = await db.Enrollments.SingleAsync(e => e.Id == enrollment.Id, ct); var view = await ViewAsync(stored, ct);
        Remember(studentId, "reserve", command.RequestId, payload, view); await db.SaveChangesAsync(ct); await tx.CommitAsync(ct); return view;
    }
    public async Task<PaymentConfirmation> ConfirmAsync(Guid studentId, Guid enrollmentId, Guid requestId, CancellationToken ct)
    {
        if (requestId == Guid.Empty) throw Fail(ErrorKind.Validation, "request.id", "Потрібен ідентифікатор запиту.");
        var payload = enrollmentId.ToString(); var previous = await ReceiptAsync<PaymentConfirmation>(studentId, "payment", requestId, payload, ct); if (previous is not null) return previous;
        var snapshot = await db.Enrollments.AsNoTracking().SingleOrDefaultAsync(e => e.Id == enrollmentId && e.StudentId == studentId, ct) ?? throw Fail(ErrorKind.NotFound, "enrollment.missing", "Запис не знайдено.");
        if (snapshot.Status == "Active") return new(true, await ViewAsync(snapshot, ct), "Запис уже підтверджено.");
        if (snapshot.Status != "Reserved") throw Fail(ErrorKind.Conflict, "enrollment.status", "Цей запис не можна підтвердити.");
        // Mock gateway is deliberately outside the group transaction/lock.
        var charge = await gateway.ChargeAsync(requestId, snapshot.PriceSnapshot, ct);
        if (!charge.Succeeded) throw Fail(ErrorKind.Conflict, "payment.failed", "Тестова оплата не пройшла.");
        await using var tx = await db.Database.BeginTransactionAsync(ct); await LockStudentAsync(studentId, ct); await LockCourseAsync(snapshot.CourseId, ct);
        previous = await ReceiptAsync<PaymentConfirmation>(studentId, "payment", requestId, payload, ct); if (previous is not null) return previous;
        var group = await repository.GetForUpdateAsync(snapshot.GroupId, ct) ?? throw Fail(ErrorKind.NotFound, "group.missing", "Групу не знайдено.");
        var current = await db.Enrollments.SingleAsync(e => e.Id == enrollmentId, ct);
        if (current.Status == "Active") return new(true, await ViewAsync(current, ct), "Запис уже підтверджено.");
        var valid = current.Status == "Reserved" && current.ReservedUntil > clock.UtcNow && !group.IsArchived;
        if (valid) group.Confirm(enrollmentId, clock.UtcNow); else group.ExpireReservations(clock.UtcNow);
        await repository.SaveAsync(group, ct);
        db.Payments.Add(new PaymentRow { StudentId = studentId, EnrollmentId = enrollmentId, RequestId = requestId, Amount = current.PriceSnapshot, GatewayReference = charge.Reference, CreatedAt = clock.UtcNow, Status = valid ? "Succeeded" : "Refunded" });
        var result = new PaymentConfirmation(valid, valid ? await ViewAsync(current, ct) : null, valid ? "Тестову оплату прийнято. Місце у групі підтверджено." : "Резерв прострочено. Тестовий платіж компенсовано; кошти не списувалися.");
        Remember(studentId, "payment", requestId, payload, result); await db.SaveChangesAsync(ct); await tx.CommitAsync(ct); return result;
    }
    public async Task CancelAsync(Guid studentId, Guid enrollmentId, CancellationToken ct)
    {
        await using var tx = await db.Database.BeginTransactionAsync(ct); await LockStudentAsync(studentId, ct);
        var row = await db.Enrollments.SingleOrDefaultAsync(e => e.Id == enrollmentId && e.StudentId == studentId, ct) ?? throw Fail(ErrorKind.NotFound, "enrollment.missing", "Запис не знайдено.");
        await LockCourseAsync(row.CourseId, ct); var group = await repository.GetForUpdateAsync(row.GroupId, ct);
        if (row.Status == "Reserved") { group!.Leave(enrollmentId); await repository.SaveAsync(group, ct); } else if (row.Status == "Active") throw Fail(ErrorKind.Conflict, "enrollment.active", "Підтверджений запис не є тимчасовим резервом.");
        await tx.CommitAsync(ct);
    }
    private async Task<EnrollmentView> ViewAsync(EnrollmentRow e, CancellationToken ct)
    {
        var course = await db.Courses.AsNoTracking().SingleAsync(c => c.Id == e.CourseId, ct); var g = await db.Groups.AsNoTracking().SingleAsync(g => g.Id == e.GroupId, ct);
        var active = await db.Enrollments.CountAsync(x => x.GroupId == g.Id && x.Status == "Active", ct);
        var occupied = await db.Enrollments.CountAsync(x => x.GroupId == g.Id && (x.Status == "Active" || x.Status == "Reserved" && x.ReservedUntil > clock.UtcNow), ct);
        var state = g.Archived ? "Archived" : active >= 5 ? "ClosedFull" : active >= 3 ? "Open" : "Forming";
        return new(e.Id, course.Id, course.Title, g.Id, g.Slot, e.Status == "Reserved" && e.ReservedUntil <= clock.UtcNow ? "Expired" : e.Status, state, active, occupied, e.ScoreAtEntry, e.PriceSnapshot, e.ReservedUntil);
    }
}
