using Microsoft.EntityFrameworkCore;
using MathHub.Infrastructure.Persistence;
using MathHub.Modules.Groups.Application;
using MathHub.Modules.Groups.Domain;
using MathHub.SharedKernel.Application;
namespace MathHub.Infrastructure.Groups;
public sealed class EfGroupRepository(MathHubDbContext db, IClock clock) : IGroupRepository
{
    public async Task<CohortGroup?> GetForUpdateAsync(Guid groupId, CancellationToken ct)
    {
        if (db.Database.CurrentTransaction is null) throw new InvalidOperationException("Group locking requires an active transaction.");
        var row = await db.Groups.FromSqlInterpolated($"SELECT * FROM groups.cohort_groups WHERE id={groupId} FOR UPDATE").SingleOrDefaultAsync(ct);
        return row is null ? null : await LoadAsync(row, ct);
    }
    public async Task<CohortGroup> LoadAsync(GroupRow row, CancellationToken ct)
    {
        var items = await db.Enrollments.Where(e => e.GroupId == row.Id).ToListAsync(ct);
        return CohortGroup.Restore(row.Id, row.CourseId, row.AgeCohort, row.CreatedAt, row.DeltaLimit, row.Archived, items.Select(e => Enrollment.Restore(e.Id, e.StudentId, e.ScoreAtEntry, e.PriceSnapshot, e.ReservedUntil, Enum.Parse<EnrollmentStatus>(e.Status))));
    }
    public Task<bool> HasActiveCourseEnrollmentAsync(Guid studentId, Guid courseId, CancellationToken ct) => db.Enrollments.AnyAsync(e => e.StudentId == studentId && e.CourseId == courseId && (e.Status == "Active" || e.Status == "Reserved" && e.ReservedUntil > clock.UtcNow), ct);
    public async Task SaveAsync(CohortGroup group, CancellationToken ct)
    {
        var items = await db.Enrollments.Where(e => e.GroupId == group.Id).ToDictionaryAsync(e => e.Id, ct);
        foreach (var item in group.Enrollments)
        {
            if (!items.TryGetValue(item.Id, out var row))
            { row = new EnrollmentRow { Id = item.Id, StudentId = item.StudentId, CourseId = group.CourseId, GroupId = group.Id, CreatedAt = clock.UtcNow }; db.Enrollments.Add(row); }
            row.Status = item.Status.ToString(); row.ScoreAtEntry = item.ScoreAtEntry; row.PriceSnapshot = item.PriceSnapshot; row.ReservedUntil = item.ReservedUntil;
        }
        await db.SaveChangesAsync(ct);
    }
}
