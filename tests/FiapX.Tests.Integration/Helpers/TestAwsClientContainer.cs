using DotNet.Testcontainers.Builders;
using DotNet.Testcontainers.Configurations;
using Testcontainers.LocalStack;
using System.IO;

namespace FiapX.Tests.Integration.Helpers;

public sealed class TestAwsClientContainer : IAsyncDisposable
{
    private readonly LocalStackContainer _container = new LocalStackBuilder("localstack/localstack:3")
        .WithName($"testcontainers-aws-{Guid.NewGuid()}")
        .WithEnvironment("SERVICES", "s3,sqs,dynamodb")
        .WithEnvironment("AWS_DEFAULT_REGION", "us-east-1")
        .WithEnvironment("FIAPX_PROJECT", "fiapx")
        .WithEnvironment("FIAPX_ENVIRONMENT", "dev")
        .WithEnvironment("LOCALSTACK_ACCOUNT_ID", "000000000000")
        .WithResourceMapping(
            new DirectoryInfo(Path.Combine(AppContext.BaseDirectory, "scripts", "localstack")),
            "/etc/localstack/init/ready.d",
            0,
            0,
            UnixFileModes.UserRead | UnixFileModes.UserExecute |
            UnixFileModes.GroupRead | UnixFileModes.GroupExecute)
        .WithWaitStrategy(Wait.ForUnixContainer().UntilMessageIsLogged("Ready."))
        .WithCleanUp(true)
        .Build();

    public string Endpoint { get; private set; } = null!;

    public async Task StartAsync()
    {
        await _container.StartAsync();
        Endpoint = $"http://localhost:{_container.GetMappedPublicPort(4566)}";
    }

    public async ValueTask DisposeAsync()
    {
        await _container.DisposeAsync();
    }
}
