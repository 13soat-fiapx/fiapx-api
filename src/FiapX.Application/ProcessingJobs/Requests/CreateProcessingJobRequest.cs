namespace FiapX.Application.ProcessingJobs.Requests;

public sealed record CreateProcessingJobRequest
{
    /// <summary>Original video file metadata used to prepare the direct S3 upload.</summary>
    public required RequestedFileRequest InputFile { get; init; }

    /// <summary>Optional description supplied by the authenticated user.</summary>
    public string? Description { get; init; }

    /// <summary>Optional author or owner label supplied by the authenticated user.</summary>
    public string? Author { get; init; }

    /// <summary>Optional client-side business reference for idempotency and tracking.</summary>
    public string? ClientReference { get; init; }
}

public sealed record RequestedFileRequest
{
    /// <summary>Original file name informed before upload.</summary>
    public required string OriginalFileName { get; init; }

    /// <summary>Video MIME type informed before upload.</summary>
    public required string ContentType { get; init; }

    /// <summary>Expected size of the original video in bytes.</summary>
    public required long SizeBytes { get; init; }

    /// <summary>Optional checksum calculated by the client for later comparison.</summary>
    public string? Checksum { get; init; }
}
