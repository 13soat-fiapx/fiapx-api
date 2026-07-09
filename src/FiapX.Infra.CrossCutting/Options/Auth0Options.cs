namespace FiapX.Infra.CrossCutting.Options;

public sealed class Auth0Options
{
    public const string SectionName = "Auth0";

    public string UserInfoEndpoint { get; init; } = string.Empty;
}
