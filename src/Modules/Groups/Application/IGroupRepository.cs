using MathHub.Modules.Groups.Domain;
namespace MathHub.Modules.Groups.Application;
/// <summary>Lock, read and mutate inside one PostgreSQL transaction.</summary>
public interface IGroupRepository
{
    Task<CohortGroup?> GetForUpdateAsync(Guid groupId, CancellationToken cancellationToken);
    Task<bool> HasActiveCourseEnrollmentAsync(Guid studentId, Guid courseId, CancellationToken cancellationToken);
    Task SaveAsync(CohortGroup group, CancellationToken cancellationToken);
}
