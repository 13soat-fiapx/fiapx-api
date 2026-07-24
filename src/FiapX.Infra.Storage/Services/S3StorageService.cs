using Amazon.S3;
using Amazon.S3.Model;
using FiapX.Application.Abstractions.Storage;
using FiapX.Domain.Storage;
using FiapX.Infra.Storage.Options;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace FiapX.Infra.Storage.Services;

public sealed class S3StorageService(
    IAmazonS3 s3Client,
    IOptions<StorageOptions> options,
    ILogger<S3StorageService> logger) : IStorageService
{
    private readonly StorageOptions _options = options.Value;

    public Task<PresignedUploadTarget> CreatePresignedUploadAsync(
        Guid processingJobId,
        string originalFileName,
        string contentType,
        CancellationToken cancellationToken)
    {
        var expiresAt = DateTimeOffset.UtcNow.AddMinutes(_options.UploadUrlExpirationMinutes);
        var s3Object = new S3ObjectReference(
            _options.BucketName,
            BuildInputObjectKey(processingJobId, originalFileName),
            _options.Region);

        var request = new GetPreSignedUrlRequest
        {
            BucketName = s3Object.Bucket,
            Key = s3Object.Key,
            Verb = HttpVerb.PUT,
            Expires = expiresAt.UtcDateTime,
            ContentType = contentType
        };

        var url = s3Client.GetPreSignedURL(request);

        return Task.FromResult(new PresignedUploadTarget
        {
            Method = "PUT",
            Url = ToPublicUrl(url),
            ExpiresAt = expiresAt,
            Headers = new Dictionary<string, string>
            {
                ["Content-Type"] = contentType
            },
            S3Object = s3Object
        });
    }

    public Task<PresignedDownloadTarget> CreatePresignedDownloadAsync(
        S3ObjectReference s3Object,
        string fileName,
        CancellationToken cancellationToken)
    {
        var expiresAt = DateTimeOffset.UtcNow.AddMinutes(_options.DownloadUrlExpirationMinutes);
        var request = new GetPreSignedUrlRequest
        {
            BucketName = s3Object.Bucket,
            Key = s3Object.Key,
            Verb = HttpVerb.GET,
            Expires = expiresAt.UtcDateTime
        };

        var url = s3Client.GetPreSignedURL(request);

        return Task.FromResult(new PresignedDownloadTarget
        {
            Url = ToPublicUrl(url),
            ExpiresAt = expiresAt
        });
    }

    public async Task<S3ObjectMetadata?> GetObjectMetadataAsync(
        S3ObjectReference s3Object,
        CancellationToken cancellationToken)
    {
        try
        {
            var response = await s3Client.GetObjectMetadataAsync(new GetObjectMetadataRequest
            {
                BucketName = s3Object.Bucket,
                Key = s3Object.Key
            }, cancellationToken);

            return new S3ObjectMetadata
            {
                SizeBytes = response.ContentLength,
                Checksum = response.Metadata.Keys.Contains("x-amz-meta-checksum")
                    ? response.Metadata["x-amz-meta-checksum"]
                    : null
            };
        }
        catch (AmazonS3Exception ex) when (ex.StatusCode == System.Net.HttpStatusCode.NotFound)
        {
            logger.LogWarning(
                "S3 object {ObjectKey} was not found in bucket {BucketName}",
                s3Object.Key,
                s3Object.Bucket);
            return null;
        }
    }

    private static string BuildInputObjectKey(Guid processingJobId, string originalFileName)
    {
        var extension = Path.GetExtension(originalFileName);
        if (string.IsNullOrWhiteSpace(extension))
            extension = ".bin";

        return $"videos/{processingJobId}/original{extension.ToLowerInvariant()}";
    }

    private string ToPublicUrl(string url)
    {
        if (string.IsNullOrWhiteSpace(_options.PublicServiceUrl))
            return url;

        var publicUri = new Uri(_options.PublicServiceUrl);
        var builder = new UriBuilder(url)
        {
            Scheme = publicUri.Scheme,
            Host = publicUri.Host,
            Port = publicUri.Port
        };

        return builder.Uri.ToString();
    }
}
