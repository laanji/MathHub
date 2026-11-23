namespace MathHub.Modules.Notifications.Domain;
public sealed record Notification(Guid Id, Guid UserId, string Subject, DateTimeOffset CreatedAt);
