using FiapX.Application.Abstractions.Auth;
using FiapX.Application.Abstractions.Messaging;
using FiapX.Application.Abstractions.Storage;
using FiapX.Application.ProcessingJobs.Messages;
using FiapX.Application.ProcessingJobs.Repositories;
using FiapX.Application.ProcessingJobs.Requests;
using FiapX.Application.ProcessingJobs.Results;
using FiapX.Application.Utils;
using FiapX.Domain.Base.Exceptions;
using FiapX.Domain.ProcessingJobs;

namespace FiapX.Application.ProcessingJobs.Services;

public sealed class ProcessingJobAppService(
    IProcessingJobRepository processingJobRepository,
    IStorageService storageService,
    IMessagePublisher messagePublisher,
    ICurrentUserService currentUserService) : IAppService
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

        var processingJobId = Guid.NewGuid();

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
            currentUserService.UserId,
            currentUserService.UserName,
            currentUserService.UserEmail,
            inputFile,
            normalizedIdempotencyKey,
            request.Description,
            request.Author,
            request.ClientReference);

        await processingJobRepository.SaveAsync(processingJob, cancellationToken);

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
        var processingJob = await GetProcessingJobAsync(processingJobId, cancellationToken);
        EnsureCurrentUserOwns(processingJob);
        var normalizedIdempotencyKey = NormalizeOptional(idempotencyKey);

        var metadata = await storageService.GetObjectMetadataAsync(
            processingJob.InputFile.S3Object,
            cancellationToken);

        if (metadata is null)
            throw new BusinessException("Uploaded file was not found in storage.");

        if (request.SizeBytes.HasValue && request.SizeBytes.Value != metadata.SizeBytes)
            throw new BusinessException("Uploaded file size does not match the informed size.");

        if (!string.IsNullOrWhiteSpace(request.Checksum) &&
            !string.IsNullOrWhiteSpace(metadata.Checksum) &&
            !string.Equals(request.Checksum.Trim(), metadata.Checksum.Trim(), StringComparison.OrdinalIgnoreCase))
            throw new BusinessException("Uploaded file checksum does not match the informed checksum.");

        if (processingJob.Status != ProcessingStatus.UploadPending)
        {
            if (normalizedIdempotencyKey is not null &&
                processingJob.Status is ProcessingStatus.Queued or ProcessingStatus.Processing or ProcessingStatus.Succeeded)
                return processingJob;

            throw new ConflictException(
                $"Invalid status for this operation. Expected: '{ProcessingStatus.UploadPending}'. Current: '{processingJob.Status}'.");
        }

        processingJob.ConfirmUpload();

        await processingJobRepository.SaveAsync(processingJob, cancellationToken);

        try
        {
            await messagePublisher.PublishAsync(ToRequestedMessage(processingJob), cancellationToken);
        }
        catch
        {
            processingJob.Fail("Processing request could not be queued.");

            try
            {
                await processingJobRepository.SaveAsync(processingJob, cancellationToken);
            }
            catch
            {
                // Preserve the original publish failure for the HTTP response.
            }

            throw;
        }

        return processingJob;
    }

    public async Task<ProcessingJob> GetStatusAsync(
        Guid processingJobId,
        CancellationToken cancellationToken)
    {
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

        return processingJob;
    }

    private void EnsureCurrentUserOwns(ProcessingJob processingJob)
    {
        if (!string.Equals(processingJob.UserId, currentUserService.UserId, StringComparison.Ordinal))
            throw new ForbiddenException("Processing job does not belong to the current user.");
    }

    private async Task<CreatedProcessingJobResult> ReplayCreateAsync(
        ProcessingJob processingJob,
        CreateProcessingJobRequest request,
        CancellationToken cancellationToken)
    {
        EnsureSameCreationRequest(processingJob, request);

        if (processingJob.Status != ProcessingStatus.UploadPending)
            throw new ConflictException(
                "Idempotency key has already been used by a processing job that is no longer accepting upload.");

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

    private static string? NormalizeOptional(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim();

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
