using System.Diagnostics;
using System.Diagnostics.Metrics;

namespace FiapX.Infra.Observability;

public static class FiapXTelemetry
{
    public const string SourceName = "FiapX";

    public static readonly ActivitySource ActivitySource = new(SourceName);
    public static readonly Meter Meter = new(SourceName);
}
