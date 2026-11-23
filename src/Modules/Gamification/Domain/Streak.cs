namespace MathHub.Modules.Gamification.Domain;
public sealed record Streak(Guid StudentId, int CurrentDays, DateOnly? LastActiveDay);
