using FiapX.Domain.Base.Exceptions;
using FiapX.Domain.ProcessingJobs;

namespace FiapX.Application.ProcessingJobs.Responses;

public static class ProcessingStatusContractMapper
{
    public const string UploadPending = "upload_pending";
    public const string Queued = "queued";
    public const string Processing = "processing";
    public const string Succeeded = "succeeded";
    public const string Failed = "failed";

    public static IReadOnlyCollection<string> Values { get; } =
    [
        UploadPending,
        Queued,
        Processing,
        Succeeded,
        Failed
    ];

    private static string AllowedValuesText => string.Join(", ", Values);

    public static ProcessingStatus? ToDomainOrNull(string? status)
    {
        if (string.IsNullOrWhiteSpace(status))
            return null;

        return ToDomain(status);
    }

    public static ProcessingStatus ToDomain(string status)
    {
        return status.Trim().ToLowerInvariant() switch
        {
            UploadPending => ProcessingStatus.UploadPending,
            Queued => ProcessingStatus.Queued,
            Processing => ProcessingStatus.Processing,
            Succeeded => ProcessingStatus.Succeeded,
            Failed => ProcessingStatus.Failed,
            _ => throw new BusinessException($"Status must be one of: {AllowedValuesText}.")
        };
    }

    public static string ToContract(ProcessingStatus status)
    {
        return status switch
        {
            ProcessingStatus.UploadPending => UploadPending,
            ProcessingStatus.Queued => Queued,
            ProcessingStatus.Processing => Processing,
            ProcessingStatus.Succeeded => Succeeded,
            ProcessingStatus.Failed => Failed,
            _ => throw new ArgumentOutOfRangeException(nameof(status), status, null)
        };
    }
}
