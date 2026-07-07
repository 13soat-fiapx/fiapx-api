using Amazon;
using Amazon.SQS;
using FiapX.Application.Abstractions.Messaging;
using FiapX.Infra.CrossCutting.Options;
using FiapX.Infra.Messaging.Helpers;
using FiapX.Infra.Messaging.Options;
using FiapX.Infra.Messaging.Publishers;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace FiapX.Infra.CrossCutting;

public static class MessagingExtensions
{
    public static IServiceCollection AddMessaging(this IServiceCollection services, IConfiguration configuration)
    {
        services.Configure<AwsCredentialsOptions>(configuration.GetSection("AwsCredentials"));
        services.Configure<MessagingOptions>(configuration.GetSection(nameof(MessagingOptions)));

        services.AddSingleton<IAmazonSQS>(_ =>
        {
            var options = configuration.GetSection("AwsCredentials").Get<AwsCredentialsOptions>()!;
            var config = new AmazonSQSConfig
            {
                RegionEndpoint = RegionEndpoint.GetBySystemName(options.Region)
            };

            if (options.UseLocalstack)
                config.ServiceURL = options.LocalstackUrl;

            return new AmazonSQSClient(
                AwsClientFactory.CreateCredentials(options),
                config);
        });

        services.AddSingleton<QueueUrlResolver>();
        services.AddScoped<IMessagePublisher, MessagePublisher>();

        return services;
    }
}
