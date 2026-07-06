using FiapX.Domain.Files;
using FiapX.Domain.ProcessingJobs;
using FiapX.Domain.Storage;
using FiapX.Infra.Data.Models;

namespace FiapX.Infra.Data.Mappers;

public static class ProcessingJobModelMapper
{
    private const int RetentionDays = 30;
    private const string DefaultResultFileName = "frames.zip";
    private const string DefaultResultContentType = "application/zip";
    private const string MissingChecksum = "not-provided";

    public static ProcessingJob ToDomain(this ProcessingJobModel model)
    {
        return ProcessingJob.Restore(
            Guid.Parse(model.Id),
            model.UserId,
            model.UserName,
            model.UserEmail,
            model.IdempotencyKey,
            model.Description,
            model.Author,
            model.ClientReference,
            ToDomainStatus(model.Status),
            model.InputFile.ToDomain(),
            model.OutputPrefix,
            model.ProgressPercentage,
            ParseNullableDateTimeOffset(model.EstimatedCompletionTime),
            model.ToDomainResultFile(),
            DateTimeOffset.Parse(model.CreatedAt),
            DateTimeOffset.Parse(model.UpdatedAt),
            ParseNullableDateTimeOffset(model.CompletedAt),
            model.Messages.Select(message => message.ToDomain()));
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
            InputFile = FromDomain(processingJob.InputFile),
            OutputPrefix = processingJob.OutputPrefix,
            ProgressPercentage = processingJob.ProgressPercentage,
            EstimatedCompletionTime = processingJob.EstimatedCompletionTime?.UtcDateTime.ToString("O"),
            ResultFileId = processingJob.ResultFile?.Id.ToString(),
            ResultFile = processingJob.ResultFile is null
                ? null
                : FromDomain(processingJob.ResultFile.S3Object),
            ResultSizeBytes = processingJob.ResultFile?.SizeBytes,
            ResultChecksum = processingJob.ResultFile?.Checksum,
            CreatedAt = processingJob.CreatedAt.UtcDateTime.ToString("O"),
            UpdatedAt = processingJob.UpdatedAt.UtcDateTime.ToString("O"),
            CompletedAt = processingJob.CompletedAt?.UtcDateTime.ToString("O"),
            ExpireAt = processingJob.CreatedAt.AddDays(RetentionDays).ToUnixTimeSeconds(),
            Messages = processingJob.Messages.Select(FromDomain).ToList()
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

    private static ProcessingInputFile ToDomain(this ProcessingInputFileModel model)
    {
        return new ProcessingInputFile(
            model.S3Object.ToDomain(),
            model.OriginalFileName,
            model.ContentType,
            model.SizeBytes,
            model.Checksum);
    }

    private static ProcessingInputFileModel FromDomain(ProcessingInputFile inputFile)
    {
        return new ProcessingInputFileModel
        {
            S3Object = FromDomain(inputFile.S3Object),
            OriginalFileName = inputFile.OriginalFileName,
            ContentType = inputFile.ContentType,
            SizeBytes = inputFile.SizeBytes,
            Checksum = inputFile.Checksum
        };
    }

    private static S3ObjectReference ToDomain(this S3ObjectReferenceModel model)
    {
        return new S3ObjectReference(model.Bucket, model.Key, model.Region);
    }

    private static S3ObjectReferenceModel FromDomain(S3ObjectReference s3Object)
    {
        return new S3ObjectReferenceModel
        {
            Bucket = s3Object.Bucket,
            Key = s3Object.Key,
            Region = s3Object.Region
        };
    }

    private static ProcessingMessage ToDomain(this ProcessingMessageModel model)
    {
        return ProcessingMessage.Restore(
            model.Code,
            model.Message,
            model.Severity,
            DateTimeOffset.Parse(model.CreatedAt));
    }

    private static ProcessingMessageModel FromDomain(ProcessingMessage message)
    {
        return new ProcessingMessageModel
        {
            Code = message.Code,
            Message = message.Message,
            Severity = message.Severity,
            CreatedAt = message.CreatedAt.UtcDateTime.ToString("O")
        };
    }

    private static DateTimeOffset? ParseNullableDateTimeOffset(string? value)
    {
        return string.IsNullOrWhiteSpace(value) ? null : DateTimeOffset.Parse(value);
    }

    private static FileResult? ToDomainResultFile(this ProcessingJobModel model)
    {
        if (model.ResultFile is null || string.IsNullOrWhiteSpace(model.ResultFileId))
            return null;

        var createdAt = ParseNullableDateTimeOffset(model.CompletedAt)
            ?? ParseNullableDateTimeOffset(model.UpdatedAt)
            ?? DateTimeOffset.UtcNow;

        return FileResult.Restore(
            Guid.Parse(model.ResultFileId),
            Guid.Parse(model.Id),
            ResolveResultFileName(model.ResultFile.Key),
            DefaultResultContentType,
            model.ResultSizeBytes ?? 0,
            string.IsNullOrWhiteSpace(model.ResultChecksum) ? MissingChecksum : model.ResultChecksum,
            model.ResultFile.ToDomain(),
            createdAt);
    }

    private static string ResolveResultFileName(string key)
    {
        var fileName = Path.GetFileName(key);
        return string.IsNullOrWhiteSpace(fileName) ? DefaultResultFileName : fileName;
    }
}
