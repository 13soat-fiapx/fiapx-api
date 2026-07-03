namespace FiapX.Application.ProcessingJobs.Requests;

public sealed record CompleteProcessingJobUploadRequest
{
    /// <summary>Observed uploaded object size in bytes.</summary>
    public long? SizeBytes { get; init; }

    /// <summary>Observed uploaded object checksum.</summary>
    public string? Checksum { get; init; }
}
