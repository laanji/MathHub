namespace MathHub.Modules.Learning.Domain;
public sealed record Lesson(Guid Id, Guid GroupId, Guid TeacherId, DateTimeOffset StartsAt, TimeSpan Duration);
