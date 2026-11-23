using System.Net;
using System.Security.Claims;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using MathHub.Infrastructure.Persistence;
using MathHub.Infrastructure.Security;
using MathHub.Modules.Assessment.Application;
using MathHub.Modules.Groups.Application;
using MathHub.Modules.Identity.Application;
using MathHub.SharedKernel.Application;
namespace MathHub.Api.Endpoints;
public sealed record ContextCommand(string Context);
public sealed record CodeCommand(string Code);
public sealed record PaymentCommand(Guid RequestId);
public sealed record QuestionCommand(string Context, string Text, string Topic, string[] Options, string CorrectAnswer, double Difficulty, bool Active);
public static class MvpEndpoints
{
    private const string Cookie = "mathhub.refresh";
    private static Guid User(HttpContext c) => Guid.Parse(c.User.FindFirstValue("sub")!);
    private static void CheckOrigin(HttpContext c)
    {
        var origin = c.Request.Headers.Origin.ToString();
        if (origin.Length == 0) return;
        var same = $"{c.Request.Scheme}://{c.Request.Host}";
        var env = c.RequestServices.GetRequiredService<IHostEnvironment>();
        if (origin != same && !(env.IsDevelopment() && origin is "http://127.0.0.1:5173" or "http://localhost:5173")) throw new RequestRejectedException(ErrorKind.Forbidden, "origin.invalid", "Джерело запиту не дозволене.");
    }
    private static IResult Tokens(HttpContext c, SessionTokens t)
    {
        c.Response.Cookies.Append(Cookie, t.RefreshToken, new CookieOptions { HttpOnly = true, Secure = c.Request.IsHttps, SameSite = SameSiteMode.Strict, Path = "/api/auth", MaxAge = TimeSpan.FromDays(14), IsEssential = true });
        c.Response.Headers.CacheControl = "no-store"; return Results.Ok(new { t.AccessToken, t.ExpiresAt });
    }
    public static void MapMvp(this WebApplication app)
    {
        var auth = app.MapGroup("/api/auth");
        auth.MapPost("/register", async (RegisterAccount r, HttpContext c, IIdentityService s, CancellationToken ct) => { CheckOrigin(c); return Results.Ok(await s.RegisterAsync(r, ct)); }).RequireRateLimiting("auth");
        auth.MapPost("/login", async (SignIn r, HttpContext c, IIdentityService s, CancellationToken ct) => { CheckOrigin(c); return Tokens(c, await s.SignInAsync(r, ct)); }).RequireRateLimiting("auth");
        auth.MapPost("/refresh", async (HttpContext c, IIdentityService s, CancellationToken ct) => { CheckOrigin(c); return Tokens(c, await s.RefreshAsync(c.Request.Cookies[Cookie] ?? "", ct)); }).RequireRateLimiting("auth");
        auth.MapPost("/logout", async (HttpContext c, IIdentityService s, CancellationToken ct) => { CheckOrigin(c); Guid? sid = Guid.TryParse(c.User.FindFirstValue("sid"), out var id) ? id : null; await s.LogoutAsync(c.Request.Cookies[Cookie], sid, ct); c.Response.Cookies.Delete(Cookie, new CookieOptions { Path = "/api/auth" }); return Results.NoContent(); });
        auth.MapGet("/me", async (HttpContext c, IIdentityService s, CancellationToken ct) => { c.Response.Headers.CacheControl = "no-store"; return Results.Ok(await s.GetAccountAsync(User(c), ct)); }).RequireAuthorization();
        app.MapGet("/api/contexts", () => SeedData.Contexts);
        app.MapGet("/api/courses", async (IEnrollmentService s, CancellationToken ct) => await s.GetCoursesAsync(ct));
        var students = app.MapGroup("/api/student").RequireAuthorization(p => p.RequireRole("Student"));
        students.MapPost("/tests", async (ContextCommand r, HttpContext c, IAssessmentService s, CancellationToken ct) => await s.StartAsync(User(c), r.Context, ct));
        students.MapGet("/tests/current", async (HttpContext c, IAssessmentService s, CancellationToken ct) => Results.Ok(await s.GetCurrentAsync(User(c), ct)));
        students.MapGet("/tests/{id:guid}", async (Guid id, HttpContext c, IAssessmentService s, CancellationToken ct) => await s.GetAsync(User(c), id, ct));
        students.MapPost("/tests/{id:guid}/answers", async (Guid id, AnswerCommand r, HttpContext c, IAssessmentService s, CancellationToken ct) => await s.SubmitAsync(User(c), id, r, ct));
        students.MapGet("/enrollments", async (HttpContext c, IEnrollmentService s, CancellationToken ct) => await s.GetMineAsync(User(c), ct));
        students.MapPost("/enrollments", async (ReserveCommand r, HttpContext c, IEnrollmentService s, CancellationToken ct) => await s.ReserveAsync(User(c), r, ct));
        students.MapDelete("/enrollments/{id:guid}", async (Guid id, HttpContext c, IEnrollmentService s, CancellationToken ct) => { await s.CancelAsync(User(c), id, ct); return Results.NoContent(); });
        students.MapPost("/enrollments/{id:guid}/payment", async (Guid id, PaymentCommand r, HttpContext c, MathHubDbContext db, IEnrollmentService s, CancellationToken ct) =>
        {
            var student = await db.Accounts.SingleAsync(u => u.Id == User(c), ct);
            if (IdentityService.Age(student.BirthDate, DateTimeOffset.UtcNow) < 16) throw new RequestRejectedException(ErrorKind.Forbidden, "payment.parent", "Підтвердження оплати виконує хтось із батьків у своєму кабінеті.");
            return await s.ConfirmAsync(student.Id, id, r.RequestId, ct);
        });
        var parents = app.MapGroup("/api/parent").RequireAuthorization(p => p.RequireRole("Parent"));
        parents.MapGet("/children", async (HttpContext c, IIdentityService s, CancellationToken ct) => await s.GetChildrenAsync(User(c), ct));
        parents.MapPost("/children/{id:guid}/confirm", async (Guid id, CodeCommand r, HttpContext c, IIdentityService s, CancellationToken ct) => { await s.ConfirmChildAsync(User(c), id, r.Code ?? "", ct); return Results.NoContent(); });
        parents.MapGet("/children/{id:guid}/enrollments", async (Guid id, HttpContext c, MathHubDbContext db, IEnrollmentService s, CancellationToken ct) => { await CheckParent(db, User(c), id, ct); return await s.GetMineAsync(id, ct); });
        parents.MapPost("/children/{childId:guid}/enrollments/{id:guid}/payment", async (Guid childId, Guid id, PaymentCommand r, HttpContext c, MathHubDbContext db, IEnrollmentService s, CancellationToken ct) => { await CheckParent(db, User(c), childId, ct); return await s.ConfirmAsync(childId, id, r.RequestId, ct); });
        var admin = app.MapGroup("/api/admin").RequireAuthorization(p => p.RequireRole("Administrator"));
        admin.MapGet("/questions", async (MathHubDbContext db, CancellationToken ct) => await db.Questions.AsNoTracking().OrderBy(q => q.Context).ThenBy(q => q.Difficulty).ToListAsync(ct));
        admin.MapPost("/questions", async (QuestionCommand r, MathHubDbContext db, CancellationToken ct) => { ValidateQuestion(r); var q = new QuestionRow { Context = r.Context, Text = r.Text, Topic = r.Topic, OptionsJson = JsonSerializer.Serialize(r.Options), CorrectAnswer = r.CorrectAnswer, Difficulty = r.Difficulty, Active = r.Active }; db.Questions.Add(q); await db.SaveChangesAsync(ct); return Results.Created($"/api/admin/questions/{q.Id}", q); });
        admin.MapPut("/questions/{id:guid}", async (Guid id, QuestionCommand r, MathHubDbContext db, CancellationToken ct) =>
        {
            ValidateQuestion(r); var old = await db.Questions.SingleOrDefaultAsync(q => q.Id == id, ct) ?? throw new RequestRejectedException(ErrorKind.NotFound, "question.missing", "Завдання не знайдено.");
            // Published questions are immutable. Existing tests keep their original statement and answer key.
            old.Active = false; var q = new QuestionRow { Context = r.Context, Text = r.Text, Topic = r.Topic, OptionsJson = JsonSerializer.Serialize(r.Options), CorrectAnswer = r.CorrectAnswer, Difficulty = r.Difficulty, Active = r.Active, Version = old.Version + 1 }; db.Questions.Add(q); await db.SaveChangesAsync(ct); return Results.Ok(q);
        });
    }
    private static void ValidateQuestion(QuestionCommand r)
    {
        if (!SeedData.Contexts.Any(c => c.Id == r.Context) || string.IsNullOrWhiteSpace(r.Text) || r.Text.Length > 2000 || string.IsNullOrWhiteSpace(r.Topic) || r.Topic.Length > 100 || r.Options is null || r.Options.Length != 4 || r.Options.Any(o => string.IsNullOrWhiteSpace(o) || o.Length > 200) || r.Options.Distinct().Count() != 4 || !r.Options.Contains(r.CorrectAnswer) || !double.IsFinite(r.Difficulty) || r.Difficulty is < 0 or > 100)
            throw new RequestRejectedException(ErrorKind.Validation, "question.invalid", "Потрібні контекст, текст, тема, 4 різні варіанти, правильна відповідь і складність 0–100.");
    }
    private static async Task CheckParent(MathHubDbContext db, Guid parentId, Guid childId, CancellationToken ct)
    { if (!await db.ParentLinks.AnyAsync(l => l.ParentId == parentId && l.ChildId == childId && l.ConfirmedAt != null, ct)) throw new RequestRejectedException(ErrorKind.NotFound, "parent.missing", "Підтверджений зв’язок із дитиною не знайдено."); }
    public static void MapDevelopmentMailbox(this WebApplication app)
    {
        app.MapGet("/api/dev/mail/{childId:guid}", async (Guid childId, HttpContext c, MathHubDbContext db, LocalMailStore mail, CancellationToken ct) =>
        {
            if (c.Connection.RemoteIpAddress is not { } ip || !IPAddress.IsLoopback(ip)) return Results.NotFound();
            var email = c.User.FindFirstValue("sub"); var parent = await db.Accounts.AsNoTracking().SingleAsync(u => u.Id == Guid.Parse(email!), ct);
            if (!await db.ParentLinks.AnyAsync(l => l.ChildId == childId && l.ParentEmail == parent.Email, ct)) return Results.NotFound();
            var message = await mail.ReadAsync(childId, ct); return message is null ? Results.NotFound() : Results.Content(message, "application/json");
        }).RequireAuthorization(p => p.RequireRole("Parent"));
    }
}
