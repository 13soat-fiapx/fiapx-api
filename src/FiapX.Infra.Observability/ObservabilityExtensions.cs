using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using OpenTelemetry.Exporter;
using OpenTelemetry.Logs;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;

namespace FiapX.Infra.Observability;

public static class ObservabilityExtensions
{
    private const string ServicePrefix = "fiapx-";

    public static IHostApplicationBuilder AddObservability(
        this IHostApplicationBuilder builder,
        ObservabilityProfile profile)
    {
        var datadog = builder.Configuration
            .GetSection(DatadogOptions.SectionName)
            .Get<DatadogOptions>() ?? new DatadogOptions();

        var appInfo = builder.Configuration
            .GetSection(AppInfoOptions.SectionName)
            .Get<AppInfoOptions>() ?? new AppInfoOptions();

        if (string.IsNullOrWhiteSpace(datadog.ApiKey))
        {
            Console.WriteLine("Datadog observability disabled: api key not configured");
            return builder;
        }

        var endpoint = datadog.OtlpEndpoint.TrimEnd('/');
        var baseHeaders = $"dd-api-key={datadog.ApiKey}";

        var resource = ResourceBuilder.CreateDefault().AddService(
            serviceName: $"{ServicePrefix}{appInfo.Name}",
            serviceVersion: appInfo.Version);

        var environmentName = builder.Environment.EnvironmentName.ToLowerInvariant();
        resource.AddAttributes(new Dictionary<string, object>
        {
            ["deployment.environment"] = environmentName
        });

        builder.Services.AddOpenTelemetry()
            .WithTracing(tracing =>
            {
                tracing
                    .SetResourceBuilder(resource)
                    .SetSampler(new AlwaysOnSampler())
                    .AddSource(FiapXTelemetry.SourceName)
                    .AddHttpClientInstrumentation();

                if (profile == ObservabilityProfile.Api)
                {
                    tracing.AddAspNetCoreInstrumentation(options =>
                    {
                        options.RecordException = true;
                        options.Filter = ctx =>
                            !ctx.Request.Path.StartsWithSegments("/health");
                    });
                }

                tracing.AddOtlpExporter(options =>
                {
                    options.Protocol = OtlpExportProtocol.HttpProtobuf;
                    options.Endpoint = new Uri($"{endpoint}/v1/traces");
                    options.Headers = $"{baseHeaders},compute_stats=true";
                });
            })
            .WithMetrics(metrics =>
            {
                metrics
                    .SetResourceBuilder(resource)
                    .AddMeter(FiapXTelemetry.SourceName)
                    .AddHttpClientInstrumentation()
                    .AddRuntimeInstrumentation();

                if (profile == ObservabilityProfile.Api)
                    metrics.AddAspNetCoreInstrumentation();

                metrics.AddOtlpExporter((options, readerOptions) =>
                {
                    options.Protocol = OtlpExportProtocol.HttpProtobuf;
                    options.Endpoint = new Uri($"{endpoint}/v1/metrics");
                    options.Headers = baseHeaders;
                    readerOptions.TemporalityPreference =
                        MetricReaderTemporalityPreference.Delta;
                });
            });

        builder.Logging.AddOpenTelemetry(logging =>
        {
            logging.SetResourceBuilder(resource);
            logging.IncludeScopes = true;
            logging.IncludeFormattedMessage = true;
            logging.ParseStateValues = true;

            logging.AddOtlpExporter(options =>
            {
                options.Protocol = OtlpExportProtocol.HttpProtobuf;
                options.Endpoint = new Uri($"{endpoint}/v1/logs");
                options.Headers = baseHeaders;
            });
        });

        return builder;
    }
}
