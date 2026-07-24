using Amazon.SQS;
using Amazon.SQS.Model;
using FiapX.Application.Abstractions.Messaging;
using FiapX.Application.ProcessingJobs.Messages;
using FiapX.Infra.Messaging.Helpers;
using FiapX.Infra.Messaging.Models;
using FiapX.Infra.Observability.Messaging;
using System.Diagnostics;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace FiapX.Infra.Messaging.Publishers;

public sealed class MessagePublisher(
    IAmazonSQS sqsClient,
    QueueUrlResolver queueUrlResolver) : IMessagePublisher
{
    private const string Source = "fiapx-api";
    private const string EventVersion = "1.0";
    private readonly JsonSerializerOptions _serializerOptions = new(JsonSerializerDefaults.Web)
    {
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
    };

    public async Task PublishAsync<TMessage>(TMessage message, CancellationToken cancellationToken)
        where TMessage : class
    {
        var eventMetadata = ResolveEventMetadata(typeof(TMessage));
        var queueUrl = await queueUrlResolver.ResolveAsync(eventMetadata.QueueLogicalName, cancellationToken);
        var queueName = queueUrl.Split('/')[^1];

        using var activity = MessageTracing.StartProducerActivity(queueName);
        if (message is VideoProcessingRequestedMessage requestedMessage)
            activity?.SetTag("video.id", requestedMessage.ProcessingJobId);

        var envelope = new MessageBase<TMessage>
        {
            Headers = BuildHeaders(eventMetadata.EventType),
            Payload = message
        };

        await sqsClient.SendMessageAsync(new SendMessageRequest
        {
            QueueUrl = queueUrl,
            MessageBody = JsonSerializer.Serialize(envelope, _serializerOptions)
        }, cancellationToken);
    }

    private static EventHeaders BuildHeaders(string eventType)
    {
        var currentActivity = Activity.Current;

        return new EventHeaders
        {
            EventId = Guid.NewGuid().ToString(),
            EventType = eventType,
            EventVersion = EventVersion,
            Traceparent = currentActivity?.Id ?? CreateTraceparent(),
            Tracestate = currentActivity?.TraceStateString,
            Baggage = currentActivity is null ? null : BuildBaggage(currentActivity),
            OccurredAt = DateTimeOffset.UtcNow,
            Source = Source
        };
    }

    private static (string QueueLogicalName, string EventType) ResolveEventMetadata(Type messageType)
    {
        if (messageType == typeof(VideoProcessingRequestedMessage))
            return ("VideoProcessingRequested", "video.processing.requested");

        throw new InvalidOperationException($"No SQS event metadata configured for message '{messageType.Name}'.");
    }

    private static string CreateTraceparent()
    {
        var traceId = ActivityTraceId.CreateRandom().ToString();
        var spanId = ActivitySpanId.CreateRandom().ToString();
        return $"00-{traceId}-{spanId}-01";
    }

    private static string? BuildBaggage(Activity activity)
    {
        var baggage = activity.Baggage
            .Select(item => $"{item.Key}={Uri.EscapeDataString(item.Value ?? string.Empty)}")
            .ToList();

        return baggage.Count == 0 ? null : string.Join(",", baggage);
    }
}

