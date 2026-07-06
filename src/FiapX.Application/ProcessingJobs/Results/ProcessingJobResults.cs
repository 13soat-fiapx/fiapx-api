using FiapX.Application.Abstractions.Storage;
using FiapX.Domain.Files;
using FiapX.Domain.ProcessingJobs;

namespace FiapX.Application.ProcessingJobs.Results;

public sealed record CreatedProcessingJobResult
{
    public required ProcessingJob ProcessingJob { get; init; }
    public required PresignedUploadTarget Upload { get; init; }
}

public sealed record PagedResult<T>
{
    public required IReadOnlyList<T> Items { get; init; }
    public required int Page { get; init; }
    public required int Size { get; init; }
    public required int Total { get; init; }
}

public sealed record FileResultMetadataResult
{
    public required FileResult FileResult { get; init; }
    public required DateTimeOffset ExpiresAt { get; init; }
}

public sealed record FileDownloadResult
{
    public required string Url { get; init; }
    public required DateTimeOffset ExpiresAt { get; init; }
}
