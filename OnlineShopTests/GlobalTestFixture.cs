using System.Reflection;

namespace OnlineShopTests;

[CollectionDefinition("Test Suite")]
public class TestSuiteCollection : ICollectionFixture<GlobalTestFixture> { }

public class GlobalTestFixture : IAsyncLifetime
{
    private readonly List<(string name, string category, int points, bool passed, string? error)> _results = new();

    private static readonly string ResultsFile = Path.Combine(
        Path.GetDirectoryName(typeof(GlobalTestFixture).Assembly.Location)!,
        "points_summary.txt");

    public void BeginTest(string testName) { }

    public void EndTest(string testName, string category, int points, bool passed, string? errorMessage = null)
    {
        lock (_results)
            _results.Add((testName, category, points, passed, errorMessage));
    }

    public (string category, int points) GetTestMetadata(string testName, object instance)
    {
        var method = instance.GetType().GetMethod(testName);
        var attr = method?.GetCustomAttribute<LoggedFactAttribute>();
        return attr != null ? (attr.Category, attr.Points) : ("General", 1);
    }

    public Task InitializeAsync() => Task.CompletedTask;

    public Task DisposeAsync()
    {
        if (_results.Count == 0)
            return Task.CompletedTask;

        var lines = new List<string>();
        lines.Add("══════════════════════════════════════════════");
        lines.Add("              POINTS SUMMARY");
        lines.Add("══════════════════════════════════════════════");

        foreach (var cat in _results.GroupBy(r => r.category).OrderBy(g => g.Key))
        {
            var earned = cat.Where(r => r.passed).Sum(r => r.points);
            var total = cat.Sum(r => r.points);
            lines.Add($"  {cat.Key,-26} {earned,3} / {total,3} pts");

            foreach (var t in cat.OrderBy(r => r.name))
            {
                var icon = t.passed ? "✓" : "✗";
                var shortName = t.name.Split('.').Last();
                var err = (!t.passed && t.error != null) ? $"  ({t.error.Split('\n')[0].Trim()})" : "";
                lines.Add($"      {icon} {shortName} ({t.points}pt){err}");
            }
        }

        lines.Add("──────────────────────────────────────────────");
        var totalEarned = _results.Where(r => r.passed).Sum(r => r.points);
        var totalPossible = _results.Sum(r => r.points);
        lines.Add($"  TOTAL:  {totalEarned} / {totalPossible} pts");
        lines.Add("══════════════════════════════════════════════");

        File.WriteAllLines(ResultsFile, lines);

        // Also write to stderr which xUnit does not capture
        foreach (var line in lines)
            Console.Error.WriteLine(line);

        return Task.CompletedTask;
    }
}