using FiapX.Domain.Base.Exceptions;
using FiapX.Domain.Storage;

namespace FiapX.Domain.ProcessingJobs;

public sealed class ProcessingInputFile
{
    /// <summary>
    /// S3 object reference for the original input video file.
    /// </summary>
    public S3ObjectReference S3Object { get; private set; } = null!;
    public string OriginalFileName { get; private set; } = string.Empty;
    /// <summary>
    /// Gets the media type of the content.
    /// </summary>
    public string ContentType { get; private set; } = string.Empty;
    public long SizeBytes { get; private set; }
    public string? Checksum { get; private set; }

    private ProcessingInputFile()
    {
    }

    public ProcessingInputFile(
        S3ObjectReference s3Object,
        string originalFileName,
        string contentType,
        long sizeBytes,
        string? checksum = null)
    {
        if (string.IsNullOrWhiteSpace(originalFileName))
            throw new BusinessException("Original file name is required.");

        if (string.IsNullOrWhiteSpace(contentType))
            throw new BusinessException("File content type is required.");

        if (sizeBytes <= 0)
            throw new BusinessException("Input file size must be greater than zero.");

        S3Object = s3Object ?? throw new BusinessException("S3 object reference is required.");
        OriginalFileName = originalFileName.Trim();
        ContentType = contentType.Trim();
        SizeBytes = sizeBytes;
        Checksum = string.IsNullOrWhiteSpace(checksum) ? null : checksum.Trim();
    }
}
