using System.Security.Claims;
using System.Text;
using System.Threading.RateLimiting;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.Extensions.FileProviders;
using MathHub.Api.Endpoints;
using MathHub.Api.Middleware;
using MathHub.Infrastructure.Assessment;
using MathHub.Infrastructure.Billing;
using MathHub.Infrastructure.Groups;
using MathHub.Infrastructure.Persistence;
using MathHub.Infrastructure.Security;
using MathHub.Modules.Assessment.Application;
using MathHub.Modules.Assessment.Domain;
using MathHub.Modules.Billing.Application;
using MathHub.Modules.Groups.Application;
using MathHub.Modules.Identity.Application;
using MathHub.SharedKernel.Application;

var builder = WebApplication.CreateBuilder(args);
var root = new DirectoryInfo(builder.Environment.ContentRootPath);
while (root.Parent is not null && !File.Exists(Path.Combine(root.FullName, "MathHub.sln"))) root = root.Parent;
builder.Configuration.AddJsonFile(Path.Combine(root.FullName, ".local", "appsettings.local.json"), optional: true, reloadOnChange: false).AddEnvironmentVariables();
var connection = builder.Configuration.GetConnectionString("MathHub") ?? throw new InvalidOperationException("Run scripts/Initialize-Local.ps1 first: PostgreSQL connection is missing.");
var jwt = builder.Configuration.GetSection("Jwt").Get<JwtSettings>() ?? throw new InvalidOperationException("JWT settings are missing.");
if (Encoding.UTF8.GetByteCount(jwt.Key) < 32) throw new InvalidOperationException("JWT key must contain at least 32 bytes.");
builder.Services.AddSingleton(jwt);
builder.Services.AddDbContext<MathHubDbContext>(o => o.UseNpgsql(connection));
builder.Services.AddProblemDetails(); builder.Services.AddExceptionHandler<BusinessRuleExceptionHandler>();
builder.Services.AddSingleton<IClock, SystemClock>(); builder.Services.AddSingleton<IScoringEngine, EloScoringEngine>();
builder.Services.AddSingleton(new LocalMailStore(Path.Combine(root.FullName, ".local", "mail")));
builder.Services.AddScoped<IIdentityService, IdentityService>(); builder.Services.AddScoped<IAssessmentService, AssessmentService>();
builder.Services.AddScoped<EfGroupRepository>(); builder.Services.AddScoped<IGroupRepository>(s => s.GetRequiredService<EfGroupRepository>());
builder.Services.AddScoped<IGroupMatchingStrategy, DeltaMatchingStrategy>(); builder.Services.AddScoped<IEnrollmentService, EnrollmentService>();
builder.Services.AddSingleton<IPaymentGateway, MockPaymentGateway>(); builder.Services.AddScoped<ScorePreviewService>();
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme).AddJwtBearer(o =>
{
    o.MapInboundClaims = false;
    o.TokenValidationParameters = new() { ValidateIssuer = true, ValidIssuer = jwt.Issuer, ValidateAudience = true, ValidAudience = jwt.Audience, ValidateIssuerSigningKey = true, IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwt.Key)), ValidateLifetime = true, ClockSkew = TimeSpan.FromSeconds(30), RoleClaimType = "role", NameClaimType = "sub", ValidAlgorithms = [SecurityAlgorithms.HmacSha256] };
    o.Events = new JwtBearerEvents
    {
        OnTokenValidated = async context =>
    {
        if (!Guid.TryParse(context.Principal?.FindFirstValue("sid"), out var sid) || !Guid.TryParse(context.Principal?.FindFirstValue("sub"), out var uid)) { context.Fail("Invalid session."); return; }
        var db = context.HttpContext.RequestServices.GetRequiredService<MathHubDbContext>(); var now = DateTimeOffset.UtcNow;
        if (!await db.AuthSessions.AnyAsync(s => s.Id == sid && s.UserId == uid && !s.Revoked && s.ExpiresAt > now, context.HttpContext.RequestAborted) || !await db.Accounts.AnyAsync(u => u.Id == uid && u.IsActive, context.HttpContext.RequestAborted)) context.Fail("Session revoked.");
    }
    };
});
builder.Services.AddAuthorization();
builder.Services.AddRateLimiter(options => { options.RejectionStatusCode = 429; options.AddPolicy("auth", context => RateLimitPartition.GetFixedWindowLimiter(context.Connection.RemoteIpAddress?.ToString() ?? "local", _ => new() { PermitLimit = 30, Window = TimeSpan.FromMinutes(1), QueueLimit = 0 })); });
var app = builder.Build();
app.UseExceptionHandler(); app.UseAuthentication(); app.UseAuthorization(); app.UseRateLimiter();
var webPath = Path.Combine(root.FullName, "src", "MathHub.Web", "dist");
if (Directory.Exists(webPath))
{
    var files = new PhysicalFileProvider(webPath);
    app.UseDefaultFiles(new DefaultFilesOptions { FileProvider = files });
    app.UseStaticFiles(new StaticFileOptions { FileProvider = files });
}
app.MapGet("/health", async (MathHubDbContext db, CancellationToken ct) => await db.Database.CanConnectAsync(ct) ? Results.Ok(new { status = "healthy", database = "PostgreSQL" }) : Results.StatusCode(503));
app.MapAssessmentPreview(); app.MapMvp();
app.MapGet("/api/config", () => new { developmentMailbox = app.Environment.IsDevelopment(), paymentsMode = "mock" });
if (app.Environment.IsDevelopment()) app.MapDevelopmentMailbox();
await using (var scope = app.Services.CreateAsyncScope())
{
    var db = scope.ServiceProvider.GetRequiredService<MathHubDbContext>(); await db.Database.MigrateAsync(); await SeedData.InitializeAsync(db);
    var adminEmail = builder.Configuration["Development:AdminEmail"]; var adminPassword = builder.Configuration["Development:AdminPassword"];
    if (app.Environment.IsDevelopment() && !string.IsNullOrWhiteSpace(adminEmail) && !string.IsNullOrWhiteSpace(adminPassword) && !await db.Accounts.AnyAsync(u => u.Email == adminEmail))
    { db.Accounts.Add(new AccountRow { Email = adminEmail, DisplayName = "Адміністратор MathHub", PasswordHash = BCrypt.Net.BCrypt.HashPassword(adminPassword, 11), Role = "Administrator", BirthDate = new DateOnly(2000, 1, 1), IsActive = true }); await db.SaveChangesAsync(); }
}
app.Run();
public partial class Program;
