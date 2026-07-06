namespace FiapX.Infra.Storage.Options;

public class StorageOptions
{
    public required string BucketName { get; init; }
    public required string Region { get; init; }
    public int UploadUrlExpirationMinutes { get; init; } = 15;
    public int DownloadUrlExpirationMinutes { get; init; } = 5;
    public string? PublicServiceUrl { get; init; }
}
