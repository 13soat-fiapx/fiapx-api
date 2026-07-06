namespace FiapX.Tests.Integration.Helpers;

[TestClass]
public static class AssemblyInitializer
{
    private static TestAwsClientContainer _awsContainer = null!;

    public static ApplicationFactory Factory { get; private set; } = null!;

    [AssemblyInitialize]
    public static async Task Setup(TestContext _)
    {
        _awsContainer = new TestAwsClientContainer();
        await _awsContainer.StartAsync();

        Factory = new ApplicationFactory(_awsContainer);
    }

    [AssemblyCleanup]
    public static async Task Cleanup()
    {
        if (Factory is not null)
            await Factory.DisposeAsync();

        if (_awsContainer is not null)
            await _awsContainer.DisposeAsync();
    }
}
