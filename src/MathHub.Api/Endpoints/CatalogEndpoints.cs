using System.Security.Claims;
using Microsoft.EntityFrameworkCore;
using MathHub.Infrastructure.Persistence;
using MathHub.Infrastructure.Security;
using MathHub.Modules.Billing.Application;
using MathHub.SharedKernel.Application;
namespace MathHub.Api.Endpoints;
public sealed record DemoCheckoutCommand(Guid CourseId, Guid RequestId, string Scenario);
public static class CatalogEndpoints
{
    private static Guid User(HttpContext c) => Guid.Parse(c.User.FindFirstValue("sub")!);
    public static void MapCatalog(this WebApplication app)
    {
        app.MapGet("/api/teachers", async (MathHubDbContext db, CancellationToken ct) =>
            await db.Teachers.AsNoTracking().OrderBy(t => t.DisplayName).ToListAsync(ct));
        var billing = app.MapGroup("/api/billing").RequireAuthorization();
        billing.MapGet("/payments", async (HttpContext c, MathHubDbContext db, CancellationToken ct) => {
            c.Response.Headers.CacheControl = "no-store";
            var uid = User(c);
            return await db.Payments.AsNoTracking().Where(p => p.StudentId == uid || db.ParentLinks.Any(l => l.ParentId == uid && l.ChildId == p.StudentId && l.ConfirmedAt != null))
                .OrderByDescending(p => p.CreatedAt).Select(p => new { p.Id, p.EnrollmentId, p.Amount, p.Status, p.GatewayReference, p.CreatedAt }).ToListAsync(ct);
        });
        billing.MapGet("/demo", async (HttpContext c, MathHubDbContext db, CancellationToken ct) => {
            c.Response.Headers.CacheControl = "no-store";
            var uid = User(c);
            return await db.DemoCheckouts.AsNoTracking().Where(p => p.UserId == uid).OrderByDescending(p => p.CreatedAt).ToListAsync(ct);
        });
        billing.MapPost("/demo", async (DemoCheckoutCommand r, HttpContext c, MathHubDbContext db, IPaymentGateway gateway, CancellationToken ct) => {
            if (r.RequestId == Guid.Empty || r.Scenario is not ("success" or "declined")) throw new RequestRejectedException(ErrorKind.Validation, "checkout.invalid", "Оберіть коректний сценарій та ідентифікатор запиту.");
            var uid = User(c);
            await using var tx = await db.Database.BeginTransactionAsync(ct);
            var user = await db.Accounts.FromSqlInterpolated($"SELECT * FROM identity.users WHERE id={uid} FOR UPDATE").SingleAsync(ct);
            if (user.Role == "Student" && IdentityService.Age(user.BirthDate, DateTimeOffset.UtcNow) < 16)
                throw new RequestRejectedException(ErrorKind.Forbidden, "payment.parent", "Демонстрацію оплати виконує хтось із батьків у своєму акаунті.");
            var previous = await db.DemoCheckouts.SingleOrDefaultAsync(p => p.UserId == uid && p.RequestId == r.RequestId, ct);
            if (previous is not null) {
                if (previous.CourseId != r.CourseId || previous.Scenario != r.Scenario) throw new RequestRejectedException(ErrorKind.Conflict, "checkout.conflict", "Цей ключ уже використано з іншими даними.");
                return Results.Ok(previous);
            }
            var course = await db.Courses.SingleOrDefaultAsync(x => x.Id == r.CourseId && x.Active, ct)
                ?? throw new RequestRejectedException(ErrorKind.NotFound, "course.missing", "Курс не знайдено.");
            var result = r.Scenario == "success" ? await gateway.ChargeAsync(r.RequestId, course.PricePerLesson, ct) : null;
            var row = new DemoCheckoutRow { UserId = uid, CourseId = course.Id, CourseTitle = course.Title, RequestId = r.RequestId,
                Amount = course.PricePerLesson, Scenario = r.Scenario, Status = result?.Succeeded == true ? "Succeeded" : "Declined",
                GatewayReference = result?.Reference ?? $"mock-declined-{r.RequestId:N}", CreatedAt = DateTimeOffset.UtcNow };
            db.DemoCheckouts.Add(row); await db.SaveChangesAsync(ct); await tx.CommitAsync(ct);
            return Results.Ok(row);
        });
    }
}
