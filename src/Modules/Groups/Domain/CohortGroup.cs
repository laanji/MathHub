using MathHub.SharedKernel.Domain;
namespace MathHub.Modules.Groups.Domain;
public enum GroupState { Forming, Open, ClosedFull, Archived }
public sealed class CohortGroup
{
    private readonly List<Enrollment> _enrollments = [];
    public Guid Id { get; private set; } = Guid.NewGuid();
    public Guid CourseId { get; private set; }
    public string AgeCohort { get; private set; } = "";
    public int DeltaLimit { get; private set; } = 5;
    public bool IsArchived { get; private set; }
    public DateTimeOffset CreatedAt { get; private set; }
    public IReadOnlyList<Enrollment> Enrollments => _enrollments.AsReadOnly();
    public int ActiveCount => _enrollments.Count(e => e.Status == EnrollmentStatus.Active);
    public GroupState State => IsArchived ? GroupState.Archived : ActiveCount >= 5 ? GroupState.ClosedFull : ActiveCount >= 3 ? GroupState.Open : GroupState.Forming;
    private CohortGroup() { }
    public CohortGroup(Guid courseId, string ageCohort, DateTimeOffset now)
    {
        if (courseId == Guid.Empty || string.IsNullOrWhiteSpace(ageCohort)) throw new BusinessRuleException("group.invalid", "Потрібні курс і вікова когорта.");
        CourseId = courseId; AgeCohort = ageCohort; CreatedAt = now;
    }
    public static CohortGroup Restore(Guid id, Guid courseId, string ageCohort, DateTimeOffset createdAt, int deltaLimit, bool archived, IEnumerable<Enrollment> enrollments)
    {
        var group = new CohortGroup(courseId, ageCohort, createdAt) { Id = id, DeltaLimit = deltaLimit, IsArchived = archived };
        group._enrollments.AddRange(enrollments);
        return group;
    }
    public int OccupiedSeats(DateTimeOffset now) => _enrollments.Count(e => e.OccupiesSeat(now));
    public int SpreadWith(int score, DateTimeOffset now)
    {
        var scores = _enrollments.Where(e => e.OccupiesSeat(now)).Select(e => e.ScoreAtEntry).Append(score).ToArray();
        return scores.Max() - scores.Min();
    }
    public bool CanAccept(int score, DateTimeOffset now) => !IsArchived && score is >= 0 and <= 100 && OccupiedSeats(now) < 5 && SpreadWith(score, now) <= DeltaLimit;
    public Enrollment Reserve(Guid studentId, int score, decimal price, DateTimeOffset now)
    {
        if (studentId == Guid.Empty || price <= 0) throw new BusinessRuleException("enrollment.invalid", "Некоректні дані запису.");
        if (_enrollments.Any(e => e.StudentId == studentId && e.OccupiesSeat(now))) throw new BusinessRuleException("enrollment.duplicate", "Учень уже займає місце.");
        if (!CanAccept(score, now)) throw new BusinessRuleException("group.unavailable", "Немає місць або перевищено допустиму різницю балів.");
        var item = new Enrollment(studentId, score, price, now); _enrollments.Add(item); return item;
    }
    public void Confirm(Guid enrollmentId, DateTimeOffset now)
    {
        var item = _enrollments.SingleOrDefault(e => e.Id == enrollmentId) ?? throw new BusinessRuleException("enrollment.missing", "Запис не знайдено.");
        if (item.Status == EnrollmentStatus.Active) return;
        if (IsArchived || item.Status != EnrollmentStatus.Reserved || item.ReservedUntil <= now)
            throw new BusinessRuleException("enrollment.expired", "Резерв уже недійсний.");
        item.Activate();
    }
    public void Leave(Guid enrollmentId) => (_enrollments.SingleOrDefault(e => e.Id == enrollmentId) ?? throw new BusinessRuleException("enrollment.missing", "Запис не знайдено.")).Leave();
    public void ExpireReservations(DateTimeOffset now) { foreach (var e in _enrollments) e.Expire(now); }
    public void ExtendDelta(DateTimeOffset waitingSince, bool consent, DateTimeOffset now)
    {
        if (!consent || now - waitingSince < TimeSpan.FromDays(7)) throw new BusinessRuleException("group.consent", "Потрібні 7 діб очікування та згода.");
        DeltaLimit = 10;
    }
    public void Archive() => IsArchived = true;
}
