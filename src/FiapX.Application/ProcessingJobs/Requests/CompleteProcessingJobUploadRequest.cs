namespace FiapX.Application.ProcessingJobs.Requests;

public sealed record CompleteProcessingJobUploadRequest
{
    public long? SizeBytes { get; init; }
    public string? Checksum { get; init; }
}
