using FiapX.Application.Abstractions.Auth;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using System.Security.Claims;

namespace FiapX.Infra.CrossCutting.Security;

public sealed class CurrentUserService(
    IHttpContextAccessor httpContextAccessor,
    IConfiguration configuration) : ICurrentUserService
{
    public string UserId => GetAuthenticatedUserId()
        ?? GetLocalUserValue("X-User-Id", "LocalUser:Id", "local-user");

    public string UserName => GetClaim(ClaimTypes.Name, "name")
        ?? GetHeader("X-User-Name")
        ?? configuration["LocalUser:Name"]
        ?? "Local User";

    public string UserEmail => GetClaim(ClaimTypes.Email, "email")
        ?? GetHeader("X-User-Email")
        ?? configuration["LocalUser:Email"]
        ?? "local.user@example.com";

    private string? GetAuthenticatedUserId()
    {
        var userId = GetClaim(ClaimTypes.NameIdentifier, "sub");
        if (!string.IsNullOrWhiteSpace(userId))
            return userId;

        if (configuration.GetValue<bool>("Authentication:Enabled"))
            throw new UnauthorizedAccessException("Authenticated token must contain the user subject claim.");

        return null;
    }

    private string GetLocalUserValue(string headerName, string configurationKey, string fallback)
    {
        return GetHeader(headerName)
            ?? configuration[configurationKey]
            ?? fallback;
    }

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
