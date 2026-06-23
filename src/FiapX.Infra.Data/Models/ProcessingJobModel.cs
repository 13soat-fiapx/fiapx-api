using Amazon.DynamoDBv2.DataModel;
using FiapX.Domain.Files;
using FiapX.Domain.ProcessingJobs;
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
    public FileResultModel? ResultFile { get; set; }
    public string CreatedAt { get; set; } = string.Empty;
    public string UpdatedAt { get; set; } = string.Empty;
    public string? CompletedAt { get; set; }
    public List<ProcessingMessageModel> Messages { get; set; } = [];

    public ProcessingJob ToDomain()
    {
        return ProcessingJob.Restore(
            Guid.Parse(Id),
            UserId,
            UserName,
            UserEmail,
            IdempotencyKey,
            Description,
            Author,
            ClientReference,
            ToDomainStatus(Status),
            InputFile.ToDomain(),
            OutputPrefix,
            ProgressPercentage,
            ParseNullableDateTimeOffset(EstimatedCompletionTime),
            ResultFile?.ToDomain(),
            DateTimeOffset.Parse(CreatedAt),
            DateTimeOffset.Parse(UpdatedAt),
            ParseNullableDateTimeOffset(CompletedAt),
            Messages.Select(message => message.ToDomain()));
    }

    public static ProcessingJobModel FromDomain(ProcessingJob processingJob)
    {
        return new ProcessingJobModel
        {
            Id = processingJob.Id.ToString(),
            UserId = processingJob.UserId,
            UserName = processingJob.UserName,
            UserEmail = processingJob.UserEmail,
            IdempotencyKey = processingJob.IdempotencyKey,
            Description = processingJob.Description,
            Author = processingJob.Author,
            ClientReference = processingJob.ClientReference,
            Status = ToStorageStatus(processingJob.Status),
            InputFile = ProcessingInputFileModel.FromDomain(processingJob.InputFile),
            OutputPrefix = processingJob.OutputPrefix,
            ProgressPercentage = processingJob.ProgressPercentage,
            EstimatedCompletionTime = processingJob.EstimatedCompletionTime?.UtcDateTime.ToString("O"),
            ResultFileId = processingJob.ResultFile?.Id.ToString(),
            ResultFile = processingJob.ResultFile is null ? null : FileResultModel.FromDomain(processingJob.ResultFile),
            CreatedAt = processingJob.CreatedAt.UtcDateTime.ToString("O"),
            UpdatedAt = processingJob.UpdatedAt.UtcDateTime.ToString("O"),
            CompletedAt = processingJob.CompletedAt?.UtcDateTime.ToString("O"),
            Messages = processingJob.Messages.Select(ProcessingMessageModel.FromDomain).ToList()
        };
    }

    public static string ToStorageStatus(ProcessingStatus status)
    {
        return status switch
        {
            ProcessingStatus.UploadPending => "upload_pending",
            ProcessingStatus.Queued => "queued",
            ProcessingStatus.Processing => "processing",
            ProcessingStatus.Succeeded => "succeeded",
            ProcessingStatus.Failed => "failed",
            _ => throw new ArgumentOutOfRangeException(nameof(status), status, null)
        };
    }

    private static ProcessingStatus ToDomainStatus(string status)
    {
        return status switch
        {
            "upload_pending" => ProcessingStatus.UploadPending,
            "queued" => ProcessingStatus.Queued,
            "processing" => ProcessingStatus.Processing,
            "succeeded" => ProcessingStatus.Succeeded,
            "failed" => ProcessingStatus.Failed,
            _ => throw new ArgumentOutOfRangeException(nameof(status), status, "Invalid processing status.")
        };
    }

    private static DateTimeOffset? ParseNullableDateTimeOffset(string? value)
    {
        return string.IsNullOrWhiteSpace(value) ? null : DateTimeOffset.Parse(value);
    }
}

public sealed class ProcessingInputFileModel
{
    public S3ObjectReferenceModel S3Object { get; set; } = new();
    public string OriginalFileName { get; set; } = string.Empty;
    public string ContentType { get; set; } = string.Empty;
    public long SizeBytes { get; set; }
    public string? Checksum { get; set; }

    public ProcessingInputFile ToDomain()
    {
        return new ProcessingInputFile(
            S3Object.ToDomain(),
            OriginalFileName,
            ContentType,
            SizeBytes,
            Checksum);
    }

    public static ProcessingInputFileModel FromDomain(ProcessingInputFile inputFile)
    {
        return new ProcessingInputFileModel
        {
            S3Object = S3ObjectReferenceModel.FromDomain(inputFile.S3Object),
            OriginalFileName = inputFile.OriginalFileName,
            ContentType = inputFile.ContentType,
            SizeBytes = inputFile.SizeBytes,
            Checksum = inputFile.Checksum
        };
    }
}

public sealed class FileResultModel
{
    public string Id { get; set; } = string.Empty;
    public string ProcessingJobId { get; set; } = string.Empty;
    public string FileName { get; set; } = string.Empty;
    public string ContentType { get; set; } = string.Empty;
    public long SizeBytes { get; set; }
    public string Checksum { get; set; } = string.Empty;
    public S3ObjectReferenceModel S3Object { get; set; } = new();
    public string CreatedAt { get; set; } = string.Empty;

    public FileResult ToDomain()
    {
        return FileResult.Restore(
            Guid.Parse(Id),
            Guid.Parse(ProcessingJobId),
            FileName,
            ContentType,
            SizeBytes,
            Checksum,
            S3Object.ToDomain(),
            DateTimeOffset.Parse(CreatedAt));
    }

    public static FileResultModel FromDomain(FileResult fileResult)
    {
        return new FileResultModel
        {
            Id = fileResult.Id.ToString(),
            ProcessingJobId = fileResult.ProcessingJobId.ToString(),
            FileName = fileResult.FileName,
            ContentType = fileResult.ContentType,
            SizeBytes = fileResult.SizeBytes,
            Checksum = fileResult.Checksum,
            S3Object = S3ObjectReferenceModel.FromDomain(fileResult.S3Object),
            CreatedAt = fileResult.CreatedAt.UtcDateTime.ToString("O")
        };
    }
}

public sealed class S3ObjectReferenceModel
{
    public string Bucket { get; set; } = string.Empty;
    public string Key { get; set; } = string.Empty;
    public string Region { get; set; } = S3ObjectReference.DefaultRegion;

    public S3ObjectReference ToDomain() => new(Bucket, Key, Region);

    public static S3ObjectReferenceModel FromDomain(S3ObjectReference s3Object)
    {
        return new S3ObjectReferenceModel
        {
            Bucket = s3Object.Bucket,
            Key = s3Object.Key,
            Region = s3Object.Region
        };
    }
}

public sealed class ProcessingMessageModel
{
    public string Code { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public string Severity { get; set; } = string.Empty;
    public string CreatedAt { get; set; } = string.Empty;

    public ProcessingMessage ToDomain()
    {
        return ProcessingMessage.Restore(
            Code,
            Message,
            Severity,
            DateTimeOffset.Parse(CreatedAt));
    }

    public static ProcessingMessageModel FromDomain(ProcessingMessage message)
    {
        return new ProcessingMessageModel
        {
            Code = message.Code,
            Message = message.Message,
            Severity = message.Severity,
            CreatedAt = message.CreatedAt.UtcDateTime.ToString("O")
        };
    }
}
