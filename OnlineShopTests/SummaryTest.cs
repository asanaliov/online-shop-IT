namespace OnlineShopTests;

/// <summary>
/// Always fails with the points summary as the message — so dotnet test always prints your score.
/// </summary>
[Collection("Test Suite")]
public class SummaryTest(GlobalTestFixture fixture) : LoggedTestBase(fixture)
{
    [Fact]
    public void PrintPointsSummary()
    {
        var file = Path.Combine(
            Path.GetDirectoryName(typeof(GlobalTestFixture).Assembly.Location)!,
            "points_summary.txt");

        var summary = File.Exists(file)
            ? string.Join(Environment.NewLine, File.ReadAllLines(file))
            : "No results written yet. Run the full test suite first.";

        Assert.Fail("\n" + summary);
    }
}