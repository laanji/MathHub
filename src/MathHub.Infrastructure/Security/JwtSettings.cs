namespace MathHub.Infrastructure.Security;
public sealed record JwtSettings(string Key, string Issuer = "MathHub", string Audience = "MathHub.Web");
