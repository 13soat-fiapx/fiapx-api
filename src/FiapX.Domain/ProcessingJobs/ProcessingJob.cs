using FiapX.Domain.Base.Exceptions;
using FiapX.Domain.Files;

namespace FiapX.Domain.ProcessingJobs;

/// <summary>
/// Represents the processing lifecycle of a video submitted by a user.
/// </summary>
public sealed class ProcessingJob
{
    private readonly List<ProcessingMessage> _messages = [];

    public Guid Id { get; private set; }
    public string UserId { get; private set; } = string.Empty;
    public string UserName { get; private set; } = string.Empty;
    public string UserEmail { get; private set; } = string.Empty;
    public string? IdempotencyKey { get; private set; }
    public string? Description { get; private set; }
    public string? Author { get; private set; }
    public string? ClientReference { get; private set; }
    public ProcessingStatus Status { get; private set; }

    /// <summary>
    /// Original video file.
    /// </summary>
    public ProcessingInputFile InputFile { get; private set; } = null!;

    /// <summary>
    /// S3 Prefix where the Zip file will be saved.
    /// </summary>
    public string OutputPrefix { get; private set; } = string.Empty;
    public decimal ProgressPercentage { get; private set; }

    /// <summary>
    /// Contract TTC (Time To Completion) for the processing job.
    /// </summary>
    public DateTimeOffset? EstimatedCompletionTime { get; private set; }

    /// <summary>
    /// Final result file containing the extracted frames in a Zip file. Only set if the processing job completed successfully.
    /// </summary>
    public FileResult? ResultFile { get; private set; }
    public DateTimeOffset CreatedAt { get; private set; }
    public DateTimeOffset UpdatedAt { get; private set; }
    public DateTimeOffset? CompletedAt { get; private set; }
    public IReadOnlyCollection<ProcessingMessage> Messages => _messages.AsReadOnly();

    private ProcessingJob()
    {
    }

    public static ProcessingJob Create(
        Guid id,
        string userId,
        string userName,
        string userEmail,
        ProcessingInputFile inputFile,
        string? idempotencyKey = null,
        string? description = null,
        string? author = null,
        string? clientReference = null)
    {
        if (id == Guid.Empty)
            throw new BusinessException("Processing job id is required.");

        if (string.IsNullOrWhiteSpace(userId))
            throw new BusinessException("User id is required.");

        if (string.IsNullOrWhiteSpace(userName))
            throw new BusinessException("User name is required.");

        if (string.IsNullOrWhiteSpace(userEmail))
            throw new BusinessException("User email is required.");

        var now = DateTimeOffset.UtcNow;

        var processingJob = new ProcessingJob
        {
            Id = id,
            UserId = userId.Trim(),
            UserName = userName.Trim(),
            UserEmail = userEmail.Trim(),
            IdempotencyKey = NormalizeOptional(idempotencyKey),
            Description = NormalizeOptional(description),
            Author = NormalizeOptional(author),
            ClientReference = NormalizeOptional(clientReference),
            Status = ProcessingStatus.UploadPending,
            InputFile = inputFile ?? throw new BusinessException("Input file is required."),
            OutputPrefix = $"frames/{id}/",
            CreatedAt = now,
            UpdatedAt = now
        };

        processingJob._messages.Add(new ProcessingMessage(
            ProcessingCode.JobCreated,
            "Processing job created and waiting for upload confirmation.",
            ProcessingMessageSeverity.Info));

        return processingJob;
    }

    public static ProcessingJob Restore(
        Guid id,
        string userId,
        string userName,
        string userEmail,
        string? idempotencyKey,
        string? description,
        string? author,
        string? clientReference,
        ProcessingStatus status,
        ProcessingInputFile inputFile,
        string outputPrefix,
        decimal progressPercentage,
        DateTimeOffset? estimatedCompletionTime,
        FileResult? resultFile,
        DateTimeOffset createdAt,
        DateTimeOffset updatedAt,
        DateTimeOffset? completedAt,
        IEnumerable<ProcessingMessage> messages)
    {
        if (id == Guid.Empty)
            throw new BusinessException("Processing job id is required.");

        if (string.IsNullOrWhiteSpace(userId))
            throw new BusinessException("User id is required.");

        if (string.IsNullOrWhiteSpace(userName))
            throw new BusinessException("User name is required.");

        if (string.IsNullOrWhiteSpace(userEmail))
            throw new BusinessException("User email is required.");

        if (string.IsNullOrWhiteSpace(outputPrefix))
            throw new BusinessException("Output prefix is required.");

        if (progressPercentage is < 0 or > 100)
            throw new BusinessException("Progress percentage must be between 0 and 100.");

        var processingJob = new ProcessingJob
        {
            Id = id,
            UserId = userId.Trim(),
            UserName = userName.Trim(),
            UserEmail = userEmail.Trim(),
            IdempotencyKey = NormalizeOptional(idempotencyKey),
            Description = NormalizeOptional(description),
            Author = NormalizeOptional(author),
            ClientReference = NormalizeOptional(clientReference),
            Status = status,
            InputFile = inputFile ?? throw new BusinessException("Input file is required."),
            OutputPrefix = outputPrefix.Trim(),
            ProgressPercentage = progressPercentage,
            EstimatedCompletionTime = estimatedCompletionTime,
            ResultFile = resultFile,
            CreatedAt = createdAt,
            UpdatedAt = updatedAt,
            CompletedAt = completedAt
        };

        processingJob._messages.AddRange(messages ?? []);

        return processingJob;
    }

    public void ConfirmUpload()
    {
        EnsureStatus(ProcessingStatus.UploadPending);

        Status = ProcessingStatus.Queued;
        UpdatedAt = DateTimeOffset.UtcNow;
        _messages.Add(new ProcessingMessage(
            ProcessingCode.UploadConfirmed,
            "Upload confirmed and processing job queued.",
            ProcessingMessageSeverity.Info));
    }

    public void StartProcessing(DateTimeOffset? estimatedCompletionTime = null)
    {
        EnsureStatus(ProcessingStatus.Queued);

        Status = ProcessingStatus.Processing;
        ProgressPercentage = 0;
        EstimatedCompletionTime = estimatedCompletionTime;
        UpdatedAt = DateTimeOffset.UtcNow;
        _messages.Add(new ProcessingMessage(
            ProcessingCode.Started,
            "Processing started.",
            ProcessingMessageSeverity.Info));
    }

    public void UpdateProgress(decimal progressPercentage, DateTimeOffset? estimatedCompletionTime = null)
    {
        EnsureStatus(ProcessingStatus.Processing);

        if (progressPercentage is < 0 or > 100)
            throw new BusinessException("Progress percentage must be between 0 and 100.");

        ProgressPercentage = progressPercentage;
        EstimatedCompletionTime = estimatedCompletionTime;
        UpdatedAt = DateTimeOffset.UtcNow;
    }

    public void CompleteSuccessfully(FileResult resultFile)
    {
        EnsureStatus(ProcessingStatus.Processing);

        if (resultFile is null)
            throw new BusinessException("Result file is required.");

        if (resultFile.ProcessingJobId != Id)
            throw new BusinessException("Result file must belong to the current processing job.");

        ResultFile = resultFile;
        Status = ProcessingStatus.Succeeded;
        ProgressPercentage = 100;
        EstimatedCompletionTime = null;
        CompletedAt = DateTimeOffset.UtcNow;
        UpdatedAt = DateTimeOffset.UtcNow;
        _messages.Add(new ProcessingMessage(
            ProcessingCode.Completed,
            "Processing completed successfully.",
            ProcessingMessageSeverity.Info));
    }

    public void Fail(string message)
    {
        if (Status is ProcessingStatus.Succeeded or ProcessingStatus.Failed)
            throw new BusinessException("Cannot fail a processing job that is already completed.");

        Status = ProcessingStatus.Failed;
        EstimatedCompletionTime = null;
        CompletedAt = DateTimeOffset.UtcNow;
        UpdatedAt = DateTimeOffset.UtcNow;
        _messages.Add(new ProcessingMessage(
            ProcessingCode.Failed,
            message,
            ProcessingMessageSeverity.Error));
    }

    private void EnsureStatus(ProcessingStatus expectedStatus)
    {
        if (Status != expectedStatus)
            throw new ConflictException(
                $"Invalid status for this operation. Expected: '{expectedStatus}'. Current: '{Status}'.");
    }

    private static string? NormalizeOptional(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}
