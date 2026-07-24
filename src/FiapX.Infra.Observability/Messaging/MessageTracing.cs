using System.Diagnostics;

namespace FiapX.Infra.Observability.Messaging;

public static class MessageTracing
{
    public static Activity? StartProducerActivity(string queueName)
    {
        var activity = FiapXTelemetry.ActivitySource.StartActivity(
            $"{queueName} publish", ActivityKind.Producer);

        activity?.SetTag("messaging.system", "aws_sqs");
        activity?.SetTag("messaging.destination.name", queueName);
        return activity;
    }

    public static string? CurrentTraceparent() => Activity.Current?.Id;

    public static Activity? StartConsumerActivity(string queueName, string? traceparent)
    {
        var parentContext = default(ActivityContext);

        if (!string.IsNullOrWhiteSpace(traceparent))
            ActivityContext.TryParse(traceparent, null, isRemote: true, out parentContext);

        var activity = FiapXTelemetry.ActivitySource.StartActivity(
            $"{queueName} process", ActivityKind.Consumer, parentContext);

        activity?.SetTag("messaging.system", "aws_sqs");
        activity?.SetTag("messaging.destination.name", queueName);
        return activity;
    }
}
