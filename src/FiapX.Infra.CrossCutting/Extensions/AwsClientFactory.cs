using Amazon.Runtime;
using FiapX.Infra.CrossCutting.Options;

namespace FiapX.Infra.CrossCutting;

internal static class AwsClientFactory
{
    public static AWSCredentials CreateCredentials(AwsCredentialsOptions options)
    {
        if (options.UseLocalstack)
            return new BasicAWSCredentials("local", "empty-key");

        if (!string.IsNullOrWhiteSpace(options.AccessKey) &&
            !string.IsNullOrWhiteSpace(options.SecretAccessKey) &&
            !string.IsNullOrWhiteSpace(options.SessionToken))
            return new SessionAWSCredentials(options.AccessKey, options.SecretAccessKey, options.SessionToken);

        if (!string.IsNullOrWhiteSpace(options.AccessKey) &&
            !string.IsNullOrWhiteSpace(options.SecretAccessKey))
            return new BasicAWSCredentials(options.AccessKey, options.SecretAccessKey);

        throw new InvalidOperationException("AWS credentials are not configured.");
    }
}
