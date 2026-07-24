using FiapX.Infra.Observability;
using System.Diagnostics.Metrics;

namespace FiapX.Application.Observability;

public static class AppMetrics
{
    public static readonly Counter<long> VideosUploaded =
        FiapXTelemetry.Meter.CreateCounter<long>("videos.uploaded");

    public static readonly Counter<long> VideoStatusTransitions =
        FiapXTelemetry.Meter.CreateCounter<long>("videos.status_transitions");
}
