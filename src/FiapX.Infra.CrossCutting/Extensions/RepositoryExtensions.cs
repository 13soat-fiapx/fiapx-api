using Amazon;
using Amazon.DynamoDBv2;
using Amazon.DynamoDBv2.DataModel;
using FiapX.Application.ProcessingJobs.Repositories;
using FiapX.Infra.CrossCutting.Options;
using FiapX.Infra.Data.Options;
using FiapX.Infra.Data.Repositories;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace FiapX.Infra.CrossCutting;

public static class RepositoryExtensions
{
    public static IServiceCollection AddDataRepositories(this IServiceCollection services, IConfiguration configuration)
    {
        services.Configure<AwsCredentialsOptions>(configuration.GetSection("AwsCredentials"));
        services.Configure<TableNames>(configuration.GetSection(nameof(TableNames)));

        services.AddSingleton<IAmazonDynamoDB>(_ =>
        {
            var options = configuration.GetSection("AwsCredentials").Get<AwsCredentialsOptions>()!;
            var config = new AmazonDynamoDBConfig
            {
                RegionEndpoint = RegionEndpoint.GetBySystemName(options.Region)
            };

            if (options.UseLocalstack)
                config.ServiceURL = options.LocalstackUrl;

            return new AmazonDynamoDBClient(
                AwsClientFactory.CreateCredentials(options),
                config);
        });

        services.AddSingleton<IDynamoDBContext, DynamoDBContext>();
        services.AddScoped<IProcessingJobRepository, ProcessingJobRepository>();

        return services;
    }
}
