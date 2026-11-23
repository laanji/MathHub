using MathHub.Modules.Groups.Domain;
namespace MathHub.Modules.Groups.Application;
public sealed record GroupSearch(Guid CourseId, string AgeCohort, int Score);
public interface IGroupMatchingStrategy
{
    CohortGroup? Select(IEnumerable<CohortGroup> groups, GroupSearch search, DateTimeOffset now);
}
public sealed class DeltaMatchingStrategy : IGroupMatchingStrategy
{
    public CohortGroup? Select(IEnumerable<CohortGroup> groups, GroupSearch search, DateTimeOffset now) => groups
        .Where(g => g.CourseId == search.CourseId && g.AgeCohort == search.AgeCohort && g.CanAccept(search.Score, now))
        .OrderBy(g => g.SpreadWith(search.Score, now)).ThenByDescending(g => g.ActiveCount)
        .ThenBy(g => g.CreatedAt).ThenBy(g => g.Id).FirstOrDefault();
}
