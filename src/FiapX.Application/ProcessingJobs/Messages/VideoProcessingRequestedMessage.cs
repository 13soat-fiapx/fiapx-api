namespace FiapX.Application.ProcessingJobs.Messages;

public sealed record VideoProcessingRequestedMessage
{
    public required Guid ProcessingJobId { get; init; }
    public required string UserId { get; init; }
    public string? Description { get; init; }
    public string? Author { get; init; }
    public string? ClientReference { get; init; }
    public required InputFileReferenceMessage InputFile { get; init; }
    public required string OutputPrefix { get; init; }
    public required DateTimeOffset RequestedAt { get; init; }
}

public sealed record InputFileReferenceMessage
{
    public required string Bucket { get; init; }
    public required string Key { get; init; }
    public required string Region { get; init; }
    public required string OriginalFileName { get; init; }
    public required string ContentType { get; init; }
    public required long SizeBytes { get; init; }
    public string? Checksum { get; init; }
}
