namespace FiapX.Infra.Messaging.Options;

public class MessagingOptions
{
    public bool DisableConsumers { get; init; }
    public required Dictionary<string, string> QueueNames { get; init; }
}
