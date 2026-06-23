using FiapX.Domain.Base.Exceptions;
using FiapX.Domain.Storage;

namespace FiapX.Domain.Files;

public sealed class FileResult
{
    public Guid Id { get; private set; }
    public Guid ProcessingJobId { get; private set; }
    public string FileName { get; private set; } = string.Empty;
    public string ContentType { get; private set; } = string.Empty;
    public long SizeBytes { get; private set; }
    public string Checksum { get; private set; } = string.Empty;
    public S3ObjectReference S3Object { get; private set; } = null!;
    public DateTimeOffset CreatedAt { get; private set; }

    private FileResult()
    {
    }

    public FileResult(
        Guid processingJobId,
        string fileName,
        string contentType,
        long sizeBytes,
        string checksum,
        S3ObjectReference s3Object)
    {
        if (processingJobId == Guid.Empty)
            throw new BusinessException("Processing job Id is required.");

        if (string.IsNullOrWhiteSpace(fileName))
            throw new BusinessException("File Name is required.");

        if (string.IsNullOrWhiteSpace(contentType))
            throw new BusinessException("File Content type is required.");

        if (sizeBytes < 0)
            throw new BusinessException("File Size cannot be negative.");

        if (string.IsNullOrWhiteSpace(checksum))
            throw new BusinessException("File checksum is required.");

        Id = Guid.NewGuid();
        ProcessingJobId = processingJobId;
        FileName = fileName.Trim();
        ContentType = contentType.Trim();
        SizeBytes = sizeBytes;
        Checksum = checksum.Trim();
        S3Object = s3Object ?? throw new BusinessException("S3 object reference is required.");
        CreatedAt = DateTimeOffset.UtcNow;
    }

    public static FileResult Restore(
        Guid id,
        Guid processingJobId,
        string fileName,
        string contentType,
        long sizeBytes,
        string checksum,
        S3ObjectReference s3Object,
        DateTimeOffset createdAt)
    {
        if (id == Guid.Empty)
            throw new BusinessException("File result id is required.");

        if (processingJobId == Guid.Empty)
            throw new BusinessException("Processing job id is required.");

        if (string.IsNullOrWhiteSpace(fileName))
            throw new BusinessException("File name is required.");

        if (string.IsNullOrWhiteSpace(contentType))
            throw new BusinessException("File content type is required.");

        if (sizeBytes < 0)
            throw new BusinessException("File size cannot be negative.");

        if (string.IsNullOrWhiteSpace(checksum))
            throw new BusinessException("File checksum is required.");

        return new FileResult
        {
            Id = id,
            ProcessingJobId = processingJobId,
            FileName = fileName.Trim(),
            ContentType = contentType.Trim(),
            SizeBytes = sizeBytes,
            Checksum = checksum.Trim(),
            S3Object = s3Object ?? throw new BusinessException("S3 object reference is required."),
            CreatedAt = createdAt
        };
    }
}
