namespace MathHub.Modules.Identity.Application;
public sealed record RegisterAccount(string Email, string DisplayName, string Password, DateOnly BirthDate, string Role, string? ParentEmail, bool Consent);
public sealed record RegistrationResult(Guid UserId, bool RequiresParent, string Message);
public sealed record SignIn(string Email, string Password);
public sealed record SessionTokens(string AccessToken, DateTimeOffset ExpiresAt, string RefreshToken);
public sealed record AccountView(Guid Id, string Email, string DisplayName, string Role, DateOnly BirthDate, int? MathScore, string? Rank, string? SubRank);
public sealed record ChildView(Guid Id, string DisplayName, bool Confirmed, int? MathScore);
public interface IIdentityService
{
    Task<RegistrationResult> RegisterAsync(RegisterAccount request, CancellationToken ct);
    Task<SessionTokens> SignInAsync(SignIn request, CancellationToken ct);
    Task<SessionTokens> RefreshAsync(string token, CancellationToken ct);
    Task LogoutAsync(string? token, Guid? sessionId, CancellationToken ct);
    Task<AccountView> GetAccountAsync(Guid userId, CancellationToken ct);
    Task<IReadOnlyList<ChildView>> GetChildrenAsync(Guid parentId, CancellationToken ct);
    Task ConfirmChildAsync(Guid parentId, Guid childId, string code, CancellationToken ct);
}
