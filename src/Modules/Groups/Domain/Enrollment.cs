namespace MathHub.Modules.Groups.Domain;
public enum EnrollmentStatus { Reserved, Active, Expired, Left }
public sealed class Enrollment
{
    public Guid Id { get; private set; } = Guid.NewGuid();
    public Guid StudentId { get; private set; }
    public int ScoreAtEntry { get; private set; }
    public decimal PriceSnapshot { get; private set; }
    public DateTimeOffset ReservedUntil { get; private set; }
    public EnrollmentStatus Status { get; private set; } = EnrollmentStatus.Reserved;
    private Enrollment() { }
    internal Enrollment(Guid studentId, int score, decimal price, DateTimeOffset now)
    { StudentId = studentId; ScoreAtEntry = score; PriceSnapshot = price; ReservedUntil = now.AddMinutes(15); }
    public static Enrollment Restore(Guid id, Guid studentId, int score, decimal price, DateTimeOffset reservedUntil, EnrollmentStatus status)
        => new() { Id = id, StudentId = studentId, ScoreAtEntry = score, PriceSnapshot = price, ReservedUntil = reservedUntil, Status = status };
    public bool OccupiesSeat(DateTimeOffset now) => Status == EnrollmentStatus.Active || Status == EnrollmentStatus.Reserved && ReservedUntil > now;
    internal void Activate() => Status = EnrollmentStatus.Active;
    internal void Leave() => Status = EnrollmentStatus.Left;
    internal void Expire(DateTimeOffset now) { if (Status == EnrollmentStatus.Reserved && ReservedUntil <= now) Status = EnrollmentStatus.Expired; }
}
