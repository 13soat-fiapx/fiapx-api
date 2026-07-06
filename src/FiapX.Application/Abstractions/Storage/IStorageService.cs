using FiapX.Domain.Storage;

namespace FiapX.Application.Abstractions.Storage;

public interface IStorageService
{
    Task<PresignedUploadTarget> CreatePresignedUploadAsync(
        Guid processingJobId,
        string originalFileName,
        string contentType,
        CancellationToken cancellationToken);

    Task<PresignedDownloadTarget> CreatePresignedDownloadAsync(
        S3ObjectReference s3Object,
        string fileName,
        CancellationToken cancellationToken);

    Task<S3ObjectMetadata?> GetObjectMetadataAsync(
        S3ObjectReference s3Object,
        CancellationToken cancellationToken);
}

public sealed record PresignedUploadTarget
{
    public required string Method { get; init; }
    public required string Url { get; init; }
    public required DateTimeOffset ExpiresAt { get; init; }
    public IReadOnlyDictionary<string, string> Headers { get; init; } = new Dictionary<string, string>();
    public required S3ObjectReference S3Object { get; init; }
}

public sealed record PresignedDownloadTarget
{
    public required string Url { get; init; }
    public required DateTimeOffset ExpiresAt { get; init; }
}

public sealed record S3ObjectMetadata
{
    public required long SizeBytes { get; init; }
    public string? Checksum { get; init; }
}
