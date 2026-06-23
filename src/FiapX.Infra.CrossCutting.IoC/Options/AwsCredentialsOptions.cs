namespace FiapX.Infra.CrossCutting.IoC.Options;

public class AwsCredentialsOptions
{
    public required string Region { get; init; }
    public bool UseLocalstack { get; init; }
    public string? LocalstackUrl { get; init; }
    public required string AccessKey { get; init; }
    public required string SecretAccessKey { get; init; }
    public required string SessionToken { get; init; }
}
