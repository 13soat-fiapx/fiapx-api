using FiapX.Application.Abstractions.Auth;
using FiapX.Application.Abstractions.Messaging;
using FiapX.Application.Abstractions.Storage;
using FiapX.Application.Observability;
using FiapX.Application.ProcessingJobs.Messages;
using FiapX.Application.ProcessingJobs.Repositories;
using FiapX.Application.ProcessingJobs.Requests;
using FiapX.Application.ProcessingJobs.Responses;
using FiapX.Application.ProcessingJobs.Results;
using FiapX.Domain.Base.Exceptions;
using FiapX.Domain.ProcessingJobs;
using Microsoft.Extensions.Logging;
using System.Diagnostics;

namespace FiapX.Application.ProcessingJobs.Services;

public sealed class ProcessingJobAppService(
    IProcessingJobRepository processingJobRepository,
    IStorageService storageService,
    IMessagePublisher messagePublisher,
    ICurrentUserService currentUserService,
    IUserProfileService userProfileService,
    ILogger<ProcessingJobAppService> logger)
{
    public Task<CreatedProcessingJobResult> CreateAsync(
        CreateProcessingJobRequest request,
        CancellationToken cancellationToken) =>
        CreateAsync(request, null, cancellationToken);

    public async Task<CreatedProcessingJobResult> CreateAsync(
        CreateProcessingJobRequest request,
        string? idempotencyKey,
        CancellationToken cancellationToken)
    {
        var normalizedIdempotencyKey = NormalizeOptional(idempotencyKey);
        if (normalizedIdempotencyKey is not null)
        {
            var existingJob = await processingJobRepository.GetByIdempotencyKeyAsync(
                currentUserService.UserId,
                normalizedIdempotencyKey,
                cancellationToken);

            if (existingJob is not null)
                return await ReplayCreateAsync(existingJob, request, cancellationToken);
        }

        var currentUser = await userProfileService.GetCurrentUserAsync(cancellationToken);
        var processingJobId = Guid.NewGuid();
        Activity.Current?.SetTag("video.id", processingJobId);

        var upload = await storageService.CreatePresignedUploadAsync(
            processingJobId,
            request.InputFile.OriginalFileName,
            request.InputFile.ContentType,
            cancellationToken);

        var inputFile = new ProcessingInputFile(
            upload.S3Object,
            request.InputFile.OriginalFileName,
            request.InputFile.ContentType,
            request.InputFile.SizeBytes,
            request.InputFile.Checksum);

        var processingJob = ProcessingJob.Create(
            processingJobId,
            currentUser.Id,
            RequireProfileValue(currentUser.Name, "name"),
            RequireProfileValue(currentUser.Email, "email"),
            inputFile,
            normalizedIdempotencyKey,
            request.Description,
            request.Author,
            request.ClientReference);

        await processingJobRepository.SaveAsync(processingJob, cancellationToken);

        RecordStatusTransition(ProcessingStatus.UploadPending);
        logger.LogInformation(
            "Processing job {ProcessingJobId} created and waiting for upload",
            processingJob.Id);

        return new CreatedProcessingJobResult
        {
            ProcessingJob = processingJob,
            Upload = upload
        };
    }

    public async Task<ProcessingJob> CompleteUploadAsync(
        Guid processingJobId,
        CompleteProcessingJobUploadRequest request,
        CancellationToken cancellationToken) =>
        await CompleteUploadAsync(processingJobId, request, null, cancellationToken);

    public async Task<ProcessingJob> CompleteUploadAsync(
        Guid processingJobId,
        CompleteProcessingJobUploadRequest request,
        string? idempotencyKey,
        CancellationToken cancellationToken)
    {
        Activity.Current?.SetTag("video.id", processingJobId);

        var processingJob = await GetProcessingJobAsync(processingJobId, cancellationToken);
        EnsureCurrentUserOwns(processingJob);
        var normalizedIdempotencyKey = NormalizeOptional(idempotencyKey);

        var metadata = await storageService.GetObjectMetadataAsync(
            processingJob.InputFile.S3Object,
            cancellationToken);

        if (metadata is null)
        {
            logger.LogWarning(
                "Upload confirmation rejected for processing job {ProcessingJobId} because the file was not found in storage",
                processingJobId);
            throw new BusinessException("Uploaded file was not found in storage.");
        }

        if (request.SizeBytes.HasValue && request.SizeBytes.Value != metadata.SizeBytes)
        {
            logger.LogWarning(
                "Upload confirmation rejected for processing job {ProcessingJobId} because the stored size {StoredSizeBytes} does not match the informed size {InformedSizeBytes}",
                processingJobId,
                metadata.SizeBytes,
                request.SizeBytes.Value);
            throw new BusinessException("Uploaded file size does not match the informed size.");
        }

        if (!string.IsNullOrWhiteSpace(request.Checksum) &&
            !string.IsNullOrWhiteSpace(metadata.Checksum) &&
            !string.Equals(request.Checksum.Trim(), metadata.Checksum.Trim(), StringComparison.OrdinalIgnoreCase))
        {
            logger.LogWarning(
                "Upload confirmation rejected for processing job {ProcessingJobId} because the checksum does not match",
                processingJobId);
            throw new BusinessException("Uploaded file checksum does not match the informed checksum.");
        }

        if (processingJob.Status != ProcessingStatus.UploadPending)
        {
            if (normalizedIdempotencyKey is not null &&
                processingJob.Status is ProcessingStatus.Queued or ProcessingStatus.Processing or ProcessingStatus.Succeeded)
            {
                logger.LogInformation(
                    "Upload confirmation replayed for processing job {ProcessingJobId} already in status {Status}",
                    processingJobId,
                    processingJob.Status);
                return processingJob;
            }

            throw new ConflictException(
                $"Invalid status for this operation. Expected: '{ProcessingStatus.UploadPending}'. Current: '{processingJob.Status}'.");
        }

        processingJob.ConfirmUpload();

        await processingJobRepository.SaveAsync(processingJob, cancellationToken);

        AppMetrics.VideosUploaded.Add(1);
        RecordStatusTransition(ProcessingStatus.Queued);
        logger.LogInformation(
            "Upload confirmed for processing job {ProcessingJobId}, publishing processing request",
            processingJobId);

        try
        {
            await messagePublisher.PublishAsync(ToRequestedMessage(processingJob), cancellationToken);
        }
        catch (Exception publishException)
        {
            logger.LogError(
                publishException,
                "Failed to publish processing request for processing job {ProcessingJobId}",
                processingJobId);

            processingJob.Fail("Processing request could not be queued.");
            RecordStatusTransition(ProcessingStatus.Failed);

            try
            {
                await processingJobRepository.SaveAsync(processingJob, cancellationToken);
            }
            catch (Exception saveException)
            {
                // Preserve the original publish failure for the HTTP response.
                logger.LogError(
                    saveException,
                    "Failed to persist failed status for processing job {ProcessingJobId} after publish error",
                    processingJobId);
            }

            throw;
        }

        logger.LogInformation(
            "Processing job {ProcessingJobId} queued for processing",
            processingJobId);

        return processingJob;
    }

    public async Task<ProcessingJob> GetStatusAsync(
        Guid processingJobId,
        CancellationToken cancellationToken)
    {
        Activity.Current?.SetTag("video.id", processingJobId);

        var processingJob = await GetProcessingJobAsync(processingJobId, cancellationToken);
        EnsureCurrentUserOwns(processingJob);

        return processingJob;
    }

    public async Task<PagedResult<ProcessingJob>> ListAsync(
        ListProcessingJobsRequest request,
        CancellationToken cancellationToken)
    {
        var jobs = await processingJobRepository.ListByUserAsync(
            currentUserService.UserId,
            request.Status,
            request.Page,
            request.Size,
            cancellationToken);

        var total = await processingJobRepository.CountByUserAsync(
            currentUserService.UserId,
            request.Status,
            cancellationToken);

        return new PagedResult<ProcessingJob>
        {
            Items = jobs,
            Page = request.Page,
            Size = request.Size,
            Total = total
        };
    }

    public async Task<FileResultMetadataResult> GetFileMetadataAsync(
        Guid fileId,
        CancellationToken cancellationToken)
    {
        var processingJob = await GetProcessingJobByResultFileAsync(fileId, cancellationToken);
        EnsureCurrentUserOwns(processingJob);

        var resultFile = processingJob.ResultFile!;
        var download = await storageService.CreatePresignedDownloadAsync(
            resultFile.S3Object,
            resultFile.FileName,
            cancellationToken);

        return new FileResultMetadataResult
        {
            FileResult = resultFile,
            ExpiresAt = download.ExpiresAt
        };
    }

    public async Task<FileDownloadResult> GetFileDownloadAsync(
        Guid fileId,
        CancellationToken cancellationToken)
    {
        var processingJob = await GetProcessingJobByResultFileAsync(fileId, cancellationToken);
        EnsureCurrentUserOwns(processingJob);

        var resultFile = processingJob.ResultFile!;
        var download = await storageService.CreatePresignedDownloadAsync(
            resultFile.S3Object,
            resultFile.FileName,
            cancellationToken);

        logger.LogInformation(
            "Presigned download generated for result file {FileId} of processing job {ProcessingJobId}",
            fileId,
            processingJob.Id);

        return new FileDownloadResult
        {
            Url = download.Url,
            ExpiresAt = download.ExpiresAt
        };
    }

    private async Task<ProcessingJob> GetProcessingJobAsync(Guid processingJobId, CancellationToken cancellationToken)
    {
        var processingJob = await processingJobRepository.GetByIdAsync(processingJobId, cancellationToken);
        EntityNotFoundException.ThrowIfNull(processingJob, processingJobId);

        return processingJob;
    }

    private async Task<ProcessingJob> GetProcessingJobByResultFileAsync(Guid fileId, CancellationToken cancellationToken)
    {
        var processingJob = await processingJobRepository.GetByResultFileIdAsync(fileId, cancellationToken);
        EntityNotFoundException.ThrowIfNull(processingJob, fileId);

        if (processingJob.ResultFile is null || processingJob.Status != ProcessingStatus.Succeeded)
            throw new EntityNotFoundException("FileResult", fileId);

        Activity.Current?.SetTag("video.id", processingJob.Id);

        return processingJob;
    }

    private void EnsureCurrentUserOwns(ProcessingJob processingJob)
    {
        if (!string.Equals(processingJob.UserId, currentUserService.UserId, StringComparison.Ordinal))
        {
            logger.LogWarning(
                "Access denied to processing job {ProcessingJobId} because it does not belong to the current user",
                processingJob.Id);
            throw new ForbiddenException("Processing job does not belong to the current user.");
        }
    }

    private async Task<CreatedProcessingJobResult> ReplayCreateAsync(
        ProcessingJob processingJob,
        CreateProcessingJobRequest request,
        CancellationToken cancellationToken)
    {
        Activity.Current?.SetTag("video.id", processingJob.Id);

        EnsureSameCreationRequest(processingJob, request);

        if (processingJob.Status != ProcessingStatus.UploadPending)
            throw new ConflictException(
                "Idempotency key has already been used by a processing job that is no longer accepting upload.");

        logger.LogInformation(
            "Creation replayed for processing job {ProcessingJobId} matched by idempotency key",
            processingJob.Id);

        var upload = await storageService.CreatePresignedUploadAsync(
            processingJob.Id,
            processingJob.InputFile.OriginalFileName,
            processingJob.InputFile.ContentType,
            cancellationToken);

        return new CreatedProcessingJobResult
        {
            ProcessingJob = processingJob,
            Upload = upload
        };
    }

    private static void EnsureSameCreationRequest(
        ProcessingJob processingJob,
        CreateProcessingJobRequest request)
    {
        var inputFile = processingJob.InputFile;
        var sameRequest =
            string.Equals(inputFile.OriginalFileName, request.InputFile.OriginalFileName.Trim(), StringComparison.Ordinal) &&
            string.Equals(inputFile.ContentType, request.InputFile.ContentType.Trim(), StringComparison.OrdinalIgnoreCase) &&
            inputFile.SizeBytes == request.InputFile.SizeBytes &&
            EqualOptional(inputFile.Checksum, request.InputFile.Checksum, StringComparison.OrdinalIgnoreCase) &&
            EqualOptional(processingJob.Description, request.Description, StringComparison.Ordinal) &&
            EqualOptional(processingJob.Author, request.Author, StringComparison.Ordinal) &&
            EqualOptional(processingJob.ClientReference, request.ClientReference, StringComparison.Ordinal);

        if (!sameRequest)
            throw new ConflictException("Idempotency key has already been used with a different request.");
    }

    private static bool EqualOptional(string? stored, string? requested, StringComparison comparison) =>
        string.Equals(stored, NormalizeOptional(requested), comparison);

    private static void RecordStatusTransition(ProcessingStatus status) =>
        AppMetrics.VideoStatusTransitions.Add(1, new KeyValuePair<string, object?>(
            "status", ProcessingStatusContractMapper.ToContract(status)));

    private static string? NormalizeOptional(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim();

    private static string RequireProfileValue(string? value, string fieldName)
    {
        var normalizedValue = NormalizeOptional(value);
        if (normalizedValue is null)
            throw new BusinessException($"Authenticated user profile must contain '{fieldName}'.");

        return normalizedValue;
    }

    private static VideoProcessingRequestedMessage ToRequestedMessage(ProcessingJob processingJob)
    {
        return new VideoProcessingRequestedMessage
        {
            ProcessingJobId = processingJob.Id,
            UserId = processingJob.UserId,
            Description = processingJob.Description,
            Author = processingJob.Author,
            ClientReference = processingJob.ClientReference,
            InputFile = new InputFileReferenceMessage
            {
                Bucket = processingJob.InputFile.S3Object.Bucket,
                Key = processingJob.InputFile.S3Object.Key,
                Region = processingJob.InputFile.S3Object.Region,
                OriginalFileName = processingJob.InputFile.OriginalFileName,
                ContentType = processingJob.InputFile.ContentType,
                SizeBytes = processingJob.InputFile.SizeBytes,
                Checksum = processingJob.InputFile.Checksum
            },
            OutputPrefix = processingJob.OutputPrefix,
            RequestedAt = DateTimeOffset.UtcNow
        };
    }
}
