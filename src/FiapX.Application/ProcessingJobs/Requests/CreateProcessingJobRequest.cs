namespace FiapX.Application.ProcessingJobs.Requests;

public sealed record CreateProcessingJobRequest
{
    public required RequestedFileRequest InputFile { get; init; }
    public string? Description { get; init; }
    public string? Author { get; init; }
    public string? ClientReference { get; init; }
}

public sealed record RequestedFileRequest
{
    public required string OriginalFileName { get; init; }
    public required string ContentType { get; init; }
    public required long SizeBytes { get; init; }
    public string? Checksum { get; init; }
}
