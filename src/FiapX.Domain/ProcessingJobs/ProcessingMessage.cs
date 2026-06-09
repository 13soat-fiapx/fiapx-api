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

        if (!ProcessingMessageSeverity.IsValid(severity))
            throw new BusinessException("Message severity must be one of: info, warning, error.");

        Code = code.Trim();
        Message = message.Trim();
        Severity = severity;
        CreatedAt = DateTimeOffset.UtcNow;
    }
}
