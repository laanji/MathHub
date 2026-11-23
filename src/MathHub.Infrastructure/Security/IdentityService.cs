using System.IdentityModel.Tokens.Jwt;
using System.Net.Mail;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using MathHub.Infrastructure.Persistence;
using MathHub.Modules.Assessment.Domain;
using MathHub.Modules.Identity.Application;
using MathHub.SharedKernel.Application;
namespace MathHub.Infrastructure.Security;
public sealed class IdentityService(MathHubDbContext db, JwtSettings settings, IClock clock, LocalMailStore mail) : IIdentityService
{
    public static string Hash(string value) => Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(value)));
    private static RequestRejectedException Reject(ErrorKind kind, string code, string message) => new(kind, code, message);
    public static int Age(DateOnly birth, DateTimeOffset now)
    { var today = DateOnly.FromDateTime(now.UtcDateTime); var age = today.Year - birth.Year; return birth > today.AddYears(-age) ? age - 1 : age; }
    public async Task<RegistrationResult> RegisterAsync(RegisterAccount request, CancellationToken ct)
    {
        var email = (request.Email ?? "").Trim().ToLowerInvariant();
        if (!MailAddress.TryCreate(email, out var parsed) || parsed.Address != email || email.Length > 254 || string.IsNullOrWhiteSpace(request.DisplayName) || request.DisplayName.Trim().Length > 100)
            throw Reject(ErrorKind.Validation, "identity.invalid", "Вкажіть коректні ім’я та email.");
        if (string.IsNullOrEmpty(request.Password) || request.Password.Length < 10 || Encoding.UTF8.GetByteCount(request.Password) > 72)
            throw Reject(ErrorKind.Validation, "password.length", "Пароль має містити щонайменше 10 символів і не більше 72 байтів.");
        var age = Age(request.BirthDate, clock.UtcNow);
        if (age is < 5 or > 100 || request.Role is not ("Student" or "Parent") || request.Role == "Parent" && age < 16 || !request.Consent)
            throw Reject(ErrorKind.Validation, "identity.profile", "Перевірте дату народження, роль та згоду на обробку даних.");
        var parentEmail = (request.ParentEmail ?? "").Trim().ToLowerInvariant();
        if (age < 16 && request.Role == "Student" && (!MailAddress.TryCreate(parentEmail, out _) || parentEmail == email))
            throw Reject(ErrorKind.Validation, "parent.email", "Для учня до 16 років потрібен окремий email батьків або опікуна.");
        if (await db.Accounts.AnyAsync(u => u.Email == email, ct)) throw Reject(ErrorKind.Conflict, "identity.email", "Цей email уже зареєстрований.");
        var account = new AccountRow { Email = email, DisplayName = request.DisplayName.Trim(), PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.Password, 11), BirthDate = request.BirthDate, Role = request.Role, IsActive = age >= 16 || request.Role == "Parent" };
        db.Accounts.Add(account);
        string? code = null;
        if (!account.IsActive)
        {
            code = RandomNumberGenerator.GetInt32(100000, 1000000).ToString();
            db.ParentLinks.Add(new ParentLinkRow { ChildId = account.Id, ParentEmail = parentEmail, CodeHash = Hash(code), ExpiresAt = clock.UtcNow.AddHours(24) });
        }
        try { await db.SaveChangesAsync(ct); } catch (DbUpdateException ex) when (ex.InnerException is Npgsql.PostgresException { SqlState: "23505" }) { throw Reject(ErrorKind.Conflict, "identity.email", "Цей email уже зареєстрований."); }
        if (code is not null) await mail.DeliverAsync(account.Id, parentEmail, code, ct);
        return new(account.Id, !account.IsActive, account.IsActive ? "Акаунт створено. Можна увійти." : "Акаунт створено. Батьки мають зареєструватися зі своїм email та підтвердити зв’язок кодом із листа.");
    }
    public async Task<SessionTokens> SignInAsync(SignIn request, CancellationToken ct)
    {
        var email = (request.Email ?? "").Trim().ToLowerInvariant();
        await using var tx = await db.Database.BeginTransactionAsync(ct);
        var user = await db.Accounts.FromSqlInterpolated($"SELECT * FROM identity.users WHERE email={email} FOR UPDATE").SingleOrDefaultAsync(ct);
        if (user is null || user.LockedUntil > clock.UtcNow) throw Reject(ErrorKind.Unauthorized, "identity.login", "Некоректний email або пароль, або вхід тимчасово заблоковано.");
        if (string.IsNullOrEmpty(request.Password) || Encoding.UTF8.GetByteCount(request.Password) > 72 || !BCrypt.Net.BCrypt.Verify(request.Password, user.PasswordHash))
        {
            user.FailedLogins++; if (user.FailedLogins >= 5) { user.LockedUntil = clock.UtcNow.AddMinutes(15); user.FailedLogins = 0; }
            await db.SaveChangesAsync(ct); await tx.CommitAsync(ct);
            throw Reject(ErrorKind.Unauthorized, "identity.login", "Некоректний email або пароль, або вхід тимчасово заблоковано.");
        }
        if (!user.IsActive) throw Reject(ErrorKind.Forbidden, "parent.pending", "Потрібне підтвердження батьків або опікуна.");
        user.FailedLogins = 0; user.LockedUntil = null;
        var session = new AuthSessionRow { UserId = user.Id, ExpiresAt = clock.UtcNow.AddDays(14) }; db.AuthSessions.Add(session);
        var tokens = CreateTokens(user, session); await db.SaveChangesAsync(ct); await tx.CommitAsync(ct); return tokens;
    }
    private SessionTokens CreateTokens(AccountRow user, AuthSessionRow session)
    {
        var expires = clock.UtcNow.AddMinutes(60); var refresh = Convert.ToBase64String(RandomNumberGenerator.GetBytes(48));
        var claims = new[] { new Claim(JwtRegisteredClaimNames.Sub, user.Id.ToString()), new Claim("role", user.Role), new Claim("sid", session.Id.ToString()), new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()) };
        var jwt = new JwtSecurityToken(settings.Issuer, settings.Audience, claims, clock.UtcNow.UtcDateTime, expires.UtcDateTime, new SigningCredentials(new SymmetricSecurityKey(Encoding.UTF8.GetBytes(settings.Key)), SecurityAlgorithms.HmacSha256));
        db.RefreshTokens.Add(new RefreshTokenRow { SessionId = session.Id, TokenHash = Hash(refresh), ExpiresAt = session.ExpiresAt });
        return new(new JwtSecurityTokenHandler().WriteToken(jwt), expires, refresh);
    }
    public async Task<SessionTokens> RefreshAsync(string token, CancellationToken ct)
    {
        var hash = Hash(token); await using var tx = await db.Database.BeginTransactionAsync(ct);
        var item = await db.RefreshTokens.AsNoTracking().SingleOrDefaultAsync(t => t.TokenHash == hash, ct) ?? throw Reject(ErrorKind.Unauthorized, "refresh.invalid", "Сесію завершено. Увійдіть знову.");
        var session = await db.AuthSessions.FromSqlInterpolated($"SELECT * FROM identity.auth_sessions WHERE id={item.SessionId} FOR UPDATE").SingleAsync(ct);
        var current = await db.RefreshTokens.SingleAsync(t => t.Id == item.Id, ct);
        if (current.Used) { session.Revoked = true; await db.SaveChangesAsync(ct); await tx.CommitAsync(ct); throw Reject(ErrorKind.Unauthorized, "refresh.reused", "Сесію відкликано. Увійдіть знову."); }
        var user = await db.Accounts.SingleAsync(u => u.Id == session.UserId, ct);
        if (session.Revoked || session.ExpiresAt <= clock.UtcNow || !user.IsActive) throw Reject(ErrorKind.Unauthorized, "refresh.expired", "Сесію завершено. Увійдіть знову.");
        current.Used = true; var tokens = CreateTokens(user, session); await db.SaveChangesAsync(ct); await tx.CommitAsync(ct); return tokens;
    }
    public async Task LogoutAsync(string? token, Guid? sessionId, CancellationToken ct)
    {
        if (sessionId is null && token is not null) { var hash = Hash(token); sessionId = await db.RefreshTokens.Where(t => t.TokenHash == hash).Select(t => (Guid?)t.SessionId).SingleOrDefaultAsync(ct); }
        if (sessionId is not null) await db.AuthSessions.Where(s => s.Id == sessionId).ExecuteUpdateAsync(s => s.SetProperty(x => x.Revoked, true), ct);
    }
    public async Task<AccountView> GetAccountAsync(Guid userId, CancellationToken ct)
    {
        var u = await db.Accounts.AsNoTracking().SingleAsync(u => u.Id == userId, ct); var rank = u.MathScore is int score ? RankResolver.Resolve(new(score)) : null;
        return new(u.Id, u.Email, u.DisplayName, u.Role, u.BirthDate, u.MathScore, rank?.Name, rank?.SubRank);
    }
    public async Task<IReadOnlyList<ChildView>> GetChildrenAsync(Guid parentId, CancellationToken ct)
    {
        var parent = await db.Accounts.AsNoTracking().SingleAsync(u => u.Id == parentId, ct);
        return await (from link in db.ParentLinks.AsNoTracking() join child in db.Accounts.AsNoTracking() on link.ChildId equals child.Id where link.ParentEmail == parent.Email select new ChildView(child.Id, child.DisplayName, link.ConfirmedAt != null, child.MathScore)).ToListAsync(ct);
    }
    public async Task ConfirmChildAsync(Guid parentId, Guid childId, string code, CancellationToken ct)
    {
        var parent = await db.Accounts.SingleAsync(u => u.Id == parentId, ct); await using var tx = await db.Database.BeginTransactionAsync(ct);
        var link = await db.ParentLinks.FromSqlInterpolated($"SELECT * FROM identity.parent_links WHERE child_id={childId} FOR UPDATE").SingleOrDefaultAsync(ct);
        if (link is null || parent.Role != "Parent" || link.ParentEmail != parent.Email) throw Reject(ErrorKind.NotFound, "parent.missing", "Запит на підтвердження не знайдено.");
        if (link.ConfirmedAt is not null) return;
        if (link.ExpiresAt <= clock.UtcNow || link.FailedAttempts >= 5) throw Reject(ErrorKind.Validation, "parent.expired", "Строк або кількість спроб коду вичерпано.");
        if (!CryptographicOperations.FixedTimeEquals(Convert.FromHexString(link.CodeHash), Convert.FromHexString(Hash(code))))
        { link.FailedAttempts++; await db.SaveChangesAsync(ct); await tx.CommitAsync(ct); throw Reject(ErrorKind.Validation, "parent.code", "Код підтвердження неправильний."); }
        link.ParentId = parentId; link.ConfirmedAt = clock.UtcNow; link.CodeHash = "";
        var child = await db.Accounts.SingleAsync(u => u.Id == childId, ct); child.IsActive = true; await db.SaveChangesAsync(ct); await tx.CommitAsync(ct);
    }
}
