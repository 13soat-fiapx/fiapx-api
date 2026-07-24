namespace FiapX.Infra.Observability;

public sealed class AppInfoOptions
{
    public const string SectionName = "AppInfo";

    public string Name { get; init; } = "service";
    public string Version { get; init; } = "1.0.0";
}
