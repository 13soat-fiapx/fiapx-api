using FiapX.Domain.Base.Exceptions;

namespace FiapX.Domain.ProcessingJobs;

public sealed class ProcessingMessage
{
    public string Code { get; private set; } = string.Empty;
    public string Message { get; private set; } = string.Empty;
    public string Severity { get; private set; } = string.Empty;
    public DateTimeOffset CreatedAt { get; private set; }

    private ProcessingMessage()
    {
    }

    public ProcessingMessage(string code, string message, string severity)
    {
        if (string.IsNullOrWhiteSpace(code))
            throw new BusinessException("Message code is required.");

        if (string.IsNullOrWhiteSpace(message))
            throw new BusinessException("Message text is required.");

        var normalizedSeverity = severity.Trim();

        if (!ProcessingMessageSeverity.IsValid(normalizedSeverity))
            throw new BusinessException("Message severity must be one of: info, warning, error.");

        Code = code.Trim();
        Message = message.Trim();
        Severity = normalizedSeverity;
        CreatedAt = DateTimeOffset.UtcNow;
    }

    public static ProcessingMessage Restore(
        string code,
        string message,
        string severity,
        DateTimeOffset createdAt)
    {
        var processingMessage = new ProcessingMessage(code, message, severity)
        {
            CreatedAt = createdAt
        };

        return processingMessage;
    }
}
