using FiapX.Domain.Base.Exceptions;

namespace FiapX.Domain.Storage;

public sealed class S3ObjectReference
{
    public const string DefaultRegion = "us-east-1";

    public string Bucket { get; private set; } = string.Empty;
    public string Key { get; private set; } = string.Empty;
    public string Region { get; private set; } = DefaultRegion;

    private S3ObjectReference()
    {
    }

    public S3ObjectReference(string bucket, string key, string? region = null)
    {
        if (string.IsNullOrWhiteSpace(bucket))
            throw new BusinessException("S3 bucket is required.");

        if (string.IsNullOrWhiteSpace(key))
            throw new BusinessException("S3 object key is required.");

        Bucket = bucket.Trim();
        Key = key.Trim();
        Region = string.IsNullOrWhiteSpace(region) ? DefaultRegion : region.Trim();
    }
}
