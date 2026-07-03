using Amazon;
using Amazon.S3;
using FiapX.Application.Abstractions.Storage;
using FiapX.Infra.CrossCutting.Options;
using FiapX.Infra.Storage.Options;
using FiapX.Infra.Storage.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace FiapX.Infra.CrossCutting;

public static class StorageExtensions
{
    public static IServiceCollection AddStorage(this IServiceCollection services, IConfiguration configuration)
    {
        services.Configure<AwsCredentialsOptions>(configuration.GetSection("AwsCredentials"));
        services.Configure<StorageOptions>(configuration.GetSection(nameof(StorageOptions)));

        services.AddSingleton<IAmazonS3>(_ =>
        {
            var options = configuration.GetSection("AwsCredentials").Get<AwsCredentialsOptions>()!;

            return new AmazonS3Client(
                AwsClientFactory.CreateCredentials(options),
                new AmazonS3Config
                {
                    RegionEndpoint = RegionEndpoint.GetBySystemName(options.Region),
                    ServiceURL = options.UseLocalstack ? options.LocalstackUrl : null,
                    ForcePathStyle = options.UseLocalstack
                });
        });

        services.AddScoped<IStorageService, S3StorageService>();

        return services;
    }
}
