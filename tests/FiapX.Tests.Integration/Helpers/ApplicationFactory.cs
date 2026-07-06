using System.Collections.Concurrent;
using System.Net.Http.Headers;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace FiapX.Tests.Integration.Helpers;

public sealed class ApplicationFactory(TestAwsClientContainer awsContainer) : WebApplicationFactory<Program>
{
    private readonly ConcurrentDictionary<string, string> _tokens = new();

    public HttpClient GetAuthenticatedClient(string userId = "auth0|test-user")
    {
        var client = CreateClient();
        var token = _tokens.GetOrAdd(userId, _ => new TestTokenGenerator().GenerateToken(userId));
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        return client;
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Development");
        builder.ConfigureAppConfiguration((_, configurationBuilder) =>
        {
            configurationBuilder.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["AppInfo:Name"] = "api",
                ["AppInfo:Version"] = "1.0.0",
                ["AppInfo:RoutePrefix"] = "api",
                ["Authentication:Enabled"] = "true",
                ["AwsCredentials:Region"] = "us-east-1",
                ["AwsCredentials:UseLocalstack"] = "true",
                ["AwsCredentials:LocalstackUrl"] = awsContainer.Endpoint,
                ["AwsCredentials:AccessKey"] = "test",
                ["AwsCredentials:SecretAccessKey"] = "test",
                ["AwsCredentials:SessionToken"] = "test",
                ["StorageOptions:BucketName"] = "fiapx-dev-artifacts-000000000000",
                ["StorageOptions:Region"] = "us-east-1",
                ["StorageOptions:PublicServiceUrl"] = awsContainer.Endpoint,
                ["MessagingOptions:DisableConsumers"] = "true",
                ["MessagingOptions:QueueNames:VideoProcessingRequested"] = "fiapx-dev-video-processing-requested",
                ["TableNames:ProcessingJobs"] = "fiapx-dev-videos-db"
            });
        });

        builder.ConfigureServices(services =>
        {
            services.PostConfigure<JwtBearerOptions>(
                JwtBearerDefaults.AuthenticationScheme,
                options =>
                {
                    options.Authority = null;
                    options.TokenValidationParameters.ValidateIssuer = false;
                    options.TokenValidationParameters.ValidateAudience = false;
                });
        });
    }
}
