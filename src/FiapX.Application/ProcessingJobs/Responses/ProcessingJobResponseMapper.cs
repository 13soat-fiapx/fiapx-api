using FiapX.Application.ProcessingJobs.Results;
using FiapX.Domain.Files;
using FiapX.Domain.ProcessingJobs;
using FiapX.Domain.Storage;

namespace FiapX.Application.ProcessingJobs.Responses;

public static class ProcessingJobResponseMapper
{
    public static ProcessingJobCreatedResponse ToCreatedResponse(CreatedProcessingJobResult result)
    {
        var processingJob = result.ProcessingJob;
        var upload = result.Upload;

        return new ProcessingJobCreatedResponse
        {
            Id = processingJob.Id,
            Status = ProcessingStatusContractMapper.ToContract(processingJob.Status),
            InputFile = ToResponse(processingJob.InputFile.S3Object),
            Upload = new PresignedUploadResponse
            {
                Method = upload.Method,
                Url = upload.Url,
                ExpiresAt = upload.ExpiresAt,
                Headers = upload.Headers,
                Object = ToResponse(upload.S3Object)
            },
            Messages = processingJob.Messages.Select(ToResponse).ToList(),
            CreatedAt = processingJob.CreatedAt,
            Links = BuildProcessingJobLinks(processingJob)
        };
    }

    public static ProcessingJobStatusResponse ToStatusResponse(ProcessingJob processingJob)
    {
        return new ProcessingJobStatusResponse
        {
            Id = processingJob.Id,
            Status = ProcessingStatusContractMapper.ToContract(processingJob.Status),
            EstimatedCompletionTime = processingJob.EstimatedCompletionTime,
            Messages = processingJob.Messages.Select(ToResponse).ToList(),
            InputFile = ToResponse(processingJob.InputFile.S3Object),
            OutputPrefix = processingJob.OutputPrefix,
            ResultFile = processingJob.ResultFile is null ? null : ToResponse(processingJob.ResultFile.S3Object),
            ProgressPercentage = processingJob.ProgressPercentage,
            CreatedAt = processingJob.CreatedAt,
            UpdatedAt = processingJob.UpdatedAt,
            Links = BuildProcessingJobLinks(processingJob)
        };
    }

    public static PagedResponse<ProcessingJobStatusResponse> ToPagedResponse(
        PagedResult<ProcessingJob> result,
        string? status)
    {
        return new PagedResponse<ProcessingJobStatusResponse>
        {
            Items = result.Items.Select(ToStatusResponse).ToList(),
            Page = result.Page,
            Size = result.Size,
            Total = result.Total,
            Links = BuildProcessingJobListLinks(status, result.Page, result.Size)
        };
    }

    public static FileResultResponse ToFileResultResponse(FileResultMetadataResult result)
    {
        return new FileResultResponse
        {
            Id = result.FileResult.Id,
            FileName = result.FileResult.FileName,
            ContentType = result.FileResult.ContentType,
            SizeBytes = result.FileResult.SizeBytes,
            Checksum = result.FileResult.Checksum,
            Object = ToResponse(result.FileResult.S3Object),
            ExpiresAt = result.ExpiresAt,
            Links = BuildFileResultLinks(result.FileResult.Id)
        };
    }

    public static FileDownloadResponse ToFileDownloadResponse(FileDownloadResult result)
    {
        return new FileDownloadResponse
        {
            Url = result.Url,
            ExpiresAt = result.ExpiresAt
        };
    }

    private static S3ObjectResponse ToResponse(S3ObjectReference s3Object)
    {
        return new S3ObjectResponse
        {
            Bucket = s3Object.Bucket,
            Key = s3Object.Key,
            Region = s3Object.Region
        };
    }

    private static ProcessingMessageResponse ToResponse(ProcessingMessage message)
    {
        return new ProcessingMessageResponse
        {
            Code = message.Code,
            Message = message.Message,
            Severity = message.Severity
        };
    }

    private static IReadOnlyDictionary<string, LinkResponse> BuildProcessingJobLinks(ProcessingJob processingJob)
    {
        var links = new Dictionary<string, LinkResponse>
        {
            ["self"] = new()
            {
                Href = $"/v1/processing-jobs/{processingJob.Id}",
                Title = "Processing job status"
            }
        };

        if (processingJob.Status == ProcessingStatus.UploadPending)
        {
            links["complete-upload"] = new LinkResponse
            {
                Href = $"/v1/processing-jobs/{processingJob.Id}/upload-completion",
                Method = "POST",
                Title = "Confirm upload completion"
            };
        }

        if (processingJob.Status == ProcessingStatus.Succeeded && processingJob.ResultFile is not null)
        {
            links["result"] = new LinkResponse
            {
                Href = $"/v1/files/{processingJob.ResultFile.Id}",
                Title = "Processing result"
            };
        }

        return links;
    }

    private static IReadOnlyDictionary<string, LinkResponse> BuildFileResultLinks(Guid fileId)
    {
        return new Dictionary<string, LinkResponse>
        {
            ["self"] = new()
            {
                Href = $"/v1/files/{fileId}",
                Title = "File metadata"
            },
            ["content"] = new()
            {
                Href = $"/v1/files/{fileId}/content",
                Title = "Download file content"
            }
        };
    }

    private static IReadOnlyDictionary<string, LinkResponse> BuildProcessingJobListLinks(
        string? status,
        int page,
        int size)
    {
        return new Dictionary<string, LinkResponse>
        {
            ["self"] = new()
            {
                Href = BuildProcessingJobListHref(status, page, size),
                Title = "Processing jobs"
            }
        };
    }

    private static string BuildProcessingJobListHref(string? status, int page, int size)
    {
        var href = $"/v1/processing-jobs?page={page}&size={size}";
        return string.IsNullOrWhiteSpace(status) ? href : $"{href}&status={status.Trim().ToLowerInvariant()}";
    }
}
