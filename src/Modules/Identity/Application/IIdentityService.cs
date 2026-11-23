namespace MathHub.Modules.Identity.Application;
public sealed record RegisterStudent(string Email, string DisplayName, string Password, DateOnly BirthDate);
public sealed record SignIn(string Email, string Password);
public sealed record SessionTokens(string AccessToken, DateTimeOffset ExpiresAt, string RefreshToken);
public interface IIdentityService
{
    Task<Guid> RegisterStudentAsync(RegisterStudent request, CancellationToken cancellationToken);
    Task<SessionTokens> SignInAsync(SignIn request, CancellationToken cancellationToken);
}
