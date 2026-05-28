using System.Runtime.CompilerServices;

namespace OnlineShopTests;

public abstract class LoggedTestBase
{
    protected readonly GlobalTestFixture Fixture;

    protected LoggedTestBase(GlobalTestFixture fixture)
    {
        Fixture = fixture;
    }

    protected void BeginTest([CallerMemberName] string methodName = "") =>
        Fixture.BeginTest(methodName);

    protected void EndTest(bool passed, string? errorMessage = null, [CallerMemberName] string methodName = "")
    {
        var (category, points) = Fixture.GetTestMetadata(methodName, this);
        Fixture.EndTest($"{GetType().Name}.{methodName}", category, points, passed, errorMessage);
    }

    public async Task RunTestAsync(Func<Task> testAction, [CallerMemberName] string methodName = "")
    {
        BeginTest(methodName);
        try
        {
            await testAction();
            EndTest(true, null, methodName);
        }
        catch (Exception ex)
        {
            EndTest(false, ex.Message, methodName);
            throw;
        }
    }
}