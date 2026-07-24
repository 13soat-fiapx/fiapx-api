namespace FiapX.Infra.Observability;

public sealed class DatadogOptions
{
    public const string SectionName = "Datadog";

    public string OtlpEndpoint { get; init; } = string.Empty;
    public string ApiKey { get; init; } = string.Empty;
}
