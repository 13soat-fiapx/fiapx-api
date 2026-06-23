using FiapX.Application.Abstractions.Auth;
using System.Security.Claims;

namespace FiapX.Api.Security;

public sealed class CurrentUserService(
    IHttpContextAccessor httpContextAccessor,
    IConfiguration configuration) : ICurrentUserService
{
    public string UserId => GetClaim(ClaimTypes.NameIdentifier, "sub")
        ?? GetHeader("X-User-Id")
        ?? configuration["LocalUser:Id"]
        ?? "local-user";

    public string UserName => GetClaim(ClaimTypes.Name, "name")
        ?? GetHeader("X-User-Name")
        ?? configuration["LocalUser:Name"]
        ?? "Local User";

    public string UserEmail => GetClaim(ClaimTypes.Email, "email")
        ?? GetHeader("X-User-Email")
        ?? configuration["LocalUser:Email"]
        ?? "local.user@example.com";

    private string? GetClaim(params string[] claimTypes)
    {
        var user = httpContextAccessor.HttpContext?.User;
        return claimTypes
            .Select(claimType => user?.FindFirst(claimType)?.Value)
            .FirstOrDefault(value => !string.IsNullOrWhiteSpace(value));
    }

    private string? GetHeader(string name)
    {
        var headers = httpContextAccessor.HttpContext?.Request.Headers;
        if (headers is null || !headers.TryGetValue(name, out var value))
            return null;

        return string.IsNullOrWhiteSpace(value) ? null : value.ToString();
    }
}
