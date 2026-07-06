using System.Text.Json.Serialization;

namespace FiapX.Application.ProcessingJobs.Responses;

public sealed record ProcessingJobCreatedResponse
{
    public required Guid Id { get; init; }
    public required string Status { get; init; }
    public required S3ObjectResponse InputFile { get; init; }
    public required PresignedUploadResponse Upload { get; init; }
    public required IReadOnlyList<ProcessingMessageResponse> Messages { get; init; }
    [property: JsonPropertyName("_links")]
    public required IReadOnlyDictionary<string, LinkResponse> Links { get; init; }
    public required DateTimeOffset CreatedAt { get; init; }
}

public sealed record ProcessingJobStatusResponse
{
    public required Guid Id { get; init; }
    public required string Status { get; init; }
    public DateTimeOffset? EstimatedCompletionTime { get; init; }
    public required IReadOnlyList<ProcessingMessageResponse> Messages { get; init; }
    public S3ObjectResponse? InputFile { get; init; }
    public string? OutputPrefix { get; init; }
    public S3ObjectResponse? ResultFile { get; init; }
    public required decimal ProgressPercentage { get; init; }
    public required DateTimeOffset CreatedAt { get; init; }
    public required DateTimeOffset UpdatedAt { get; init; }
    [property: JsonPropertyName("_links")]
    public required IReadOnlyDictionary<string, LinkResponse> Links { get; init; }
}

public sealed record FileResultResponse
{
    public required Guid Id { get; init; }
    public required string FileName { get; init; }
    public required string ContentType { get; init; }
    public required long SizeBytes { get; init; }
    public required string Checksum { get; init; }
    public required S3ObjectResponse Object { get; init; }
    public required DateTimeOffset ExpiresAt { get; init; }
    [property: JsonPropertyName("_links")]
    public required IReadOnlyDictionary<string, LinkResponse> Links { get; init; }
}

public sealed record FileDownloadResponse
{
    public required string Url { get; init; }
    public required DateTimeOffset ExpiresAt { get; init; }
}

public sealed record PagedResponse<T>
{
    public required IReadOnlyList<T> Items { get; init; }
    public required int Page { get; init; }
    public required int Size { get; init; }
    public required int Total { get; init; }
    [property: JsonPropertyName("_links")]
    public required IReadOnlyDictionary<string, LinkResponse> Links { get; init; }
}

public sealed record PresignedUploadResponse
{
    public required string Method { get; init; }
    public required string Url { get; init; }
    public required DateTimeOffset ExpiresAt { get; init; }
    public IReadOnlyDictionary<string, string> Headers { get; init; } = new Dictionary<string, string>();
    public required S3ObjectResponse Object { get; init; }
}

public sealed record ProcessingMessageResponse
{
    public required string Code { get; init; }
    public required string Message { get; init; }
    public required string Severity { get; init; }
}

public sealed record S3ObjectResponse
{
    public required string Bucket { get; init; }
    public required string Key { get; init; }
    public required string Region { get; init; }
}

public sealed record LinkResponse
{
    public required string Href { get; init; }
    public string Method { get; init; } = "GET";
    public string? Title { get; init; }
    public string? Type { get; init; }
}
