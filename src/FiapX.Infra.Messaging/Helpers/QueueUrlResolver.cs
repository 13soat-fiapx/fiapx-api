using Amazon.SQS;
using FiapX.Infra.Messaging.Options;
using Microsoft.Extensions.Options;

namespace FiapX.Infra.Messaging.Helpers;

public sealed class QueueUrlResolver(
    IAmazonSQS sqsClient,
    IOptions<MessagingOptions> options)
{
    private readonly Dictionary<string, string> _queueNames = options.Value.QueueNames;

    public async Task<string> ResolveAsync(string logicalName, CancellationToken cancellationToken)
    {
        if (!_queueNames.TryGetValue(logicalName, out var queueName) || string.IsNullOrWhiteSpace(queueName))
            throw new InvalidOperationException($"Queue not configured for logical name '{logicalName}'.");

        var response = await sqsClient.GetQueueUrlAsync(queueName, cancellationToken);
        return response.QueueUrl;
    }
}
