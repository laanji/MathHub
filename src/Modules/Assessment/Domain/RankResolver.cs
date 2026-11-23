namespace MathHub.Modules.Assessment.Domain;
public sealed record Rank(string Code, string Name, string SubRank, int Start, int End);
public static class RankResolver
{
    private static readonly (string Code, string Name, int[] Limits)[] Ranks =
    [
        ("S", "Starter", [0, 5, 10, 15]),
        ("E", "Elementary", [15, 22, 29, 35]),
        ("I", "Intermediate", [35, 42, 49, 55]),
        ("A", "Advanced", [55, 62, 69, 75]),
        ("P", "Proficient", [75, 80, 85, 90]),
        ("M", "Master", [90, 94, 98, 101])
    ];
    public static Rank Resolve(MathScore score)
    {
        var rank = Ranks.Single(r => score.Value >= r.Limits[0] && score.Value < r.Limits[3]);
        var sub = Enumerable.Range(0, 3).Single(i => score.Value >= rank.Limits[i] && score.Value < rank.Limits[i + 1]);
        return new(rank.Code, rank.Name, new[] { "−", "Core", "+" }[sub], rank.Limits[sub], rank.Limits[sub + 1] - 1);
    }
}
