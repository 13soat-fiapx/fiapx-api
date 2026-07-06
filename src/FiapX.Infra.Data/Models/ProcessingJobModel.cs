using Amazon.DynamoDBv2.DataModel;
using FiapX.Domain.Storage;

namespace FiapX.Infra.Data.Models;

[DynamoDBTable(nameof(ProcessingJobModel), LowerCamelCaseProperties = true)]
public sealed class ProcessingJobModel
{
    [DynamoDBHashKey]
    public string Id { get; set; } = string.Empty;

    [DynamoDBGlobalSecondaryIndexHashKey("userId-index")]
    public string UserId { get; set; } = string.Empty;

    [DynamoDBGlobalSecondaryIndexHashKey("resultFileId-index")]
    public string? ResultFileId { get; set; }

    public string UserName { get; set; } = string.Empty;
    public string UserEmail { get; set; } = string.Empty;
    public string? IdempotencyKey { get; set; }
    public string? Description { get; set; }
    public string? Author { get; set; }
    public string? ClientReference { get; set; }
    public string Status { get; set; } = string.Empty;
    public ProcessingInputFileModel InputFile { get; set; } = new();
    public string OutputPrefix { get; set; } = string.Empty;
    public decimal ProgressPercentage { get; set; }
    public string? EstimatedCompletionTime { get; set; }
    public S3ObjectReferenceModel? ResultFile { get; set; }
    public long? ResultSizeBytes { get; set; }
    public string? ResultChecksum { get; set; }
    public string CreatedAt { get; set; } = string.Empty;
    public string UpdatedAt { get; set; } = string.Empty;
    public string? CompletedAt { get; set; }
    public long? ExpireAt { get; set; }
    public List<ProcessingMessageModel> Messages { get; set; } = [];
}

public sealed class ProcessingInputFileModel
{
    public S3ObjectReferenceModel S3Object { get; set; } = new();
    public string OriginalFileName { get; set; } = string.Empty;
    public string ContentType { get; set; } = string.Empty;
    public long SizeBytes { get; set; }
    public string? Checksum { get; set; }
}

public sealed class S3ObjectReferenceModel
{
    public string Bucket { get; set; } = string.Empty;
    public string Key { get; set; } = string.Empty;
    public string Region { get; set; } = S3ObjectReference.DefaultRegion;
}

public sealed class ProcessingMessageModel
{
    public string Code { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public string Severity { get; set; } = string.Empty;
    public string CreatedAt { get; set; } = string.Empty;
}
