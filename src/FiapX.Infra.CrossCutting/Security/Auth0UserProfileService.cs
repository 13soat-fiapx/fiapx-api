using FiapX.Application.Abstractions.Auth;
using FiapX.Domain.Base.Exceptions;
using FiapX.Infra.CrossCutting.Options;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Options;
using System.Net;
using System.Net.Http.Json;
using System.Text.Json.Serialization;

namespace FiapX.Infra.CrossCutting.Security;

public sealed class Auth0UserProfileService(
    HttpClient httpClient,
    IHttpContextAccessor httpContextAccessor,
    ICurrentUserService currentUserService,
    IOptions<Auth0Options> options) : IUserProfileService
{
    public async Task<UserProfile> GetCurrentUserAsync(CancellationToken cancellationToken)
    {
        var endpoint = options.Value.UserInfoEndpoint;
        if (string.IsNullOrWhiteSpace(endpoint))
            throw new InvalidOperationException("Auth0 userinfo endpoint is not configured.");

        using var request = new HttpRequestMessage(HttpMethod.Get, endpoint);
        request.Headers.TryAddWithoutValidation("Authorization", GetAuthorizationHeader());

        using var response = await httpClient.SendAsync(
            request,
            HttpCompletionOption.ResponseHeadersRead,
            cancellationToken);

        if (response.StatusCode is HttpStatusCode.Unauthorized or HttpStatusCode.Forbidden)
            throw new UnauthorizedAccessException("Auth0 rejected the access token.");

        if (!response.IsSuccessStatusCode)
            throw new InvalidOperationException(
                $"Auth0 userinfo request failed with status code {(int)response.StatusCode}.");

        var userInfo = await response.Content.ReadFromJsonAsync<Auth0UserInfoResponse>(
            cancellationToken: cancellationToken);

        if (string.IsNullOrWhiteSpace(userInfo?.Sub))
            throw new UnauthorizedAccessException("Auth0 userinfo response must contain the user subject.");

        if (!string.Equals(userInfo.Sub, currentUserService.UserId, StringComparison.Ordinal))
            throw new ForbiddenException("Auth0 userinfo response does not match the authenticated user.");

        return new UserProfile(userInfo.Sub, userInfo.Name, userInfo.Email);
    }

    private string GetAuthorizationHeader()
    {
        var authorization = httpContextAccessor.HttpContext?.Request.Headers.Authorization.ToString();
        if (string.IsNullOrWhiteSpace(authorization))
            throw new UnauthorizedAccessException("Authorization header is required.");

        if (!authorization.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase))
            throw new UnauthorizedAccessException("Bearer token is required.");

        return authorization;
    }

    private sealed record Auth0UserInfoResponse
    {
        [JsonPropertyName("sub")]
        public string? Sub { get; init; }

        [JsonPropertyName("name")]
        public string? Name { get; init; }

        [JsonPropertyName("email")]
        public string? Email { get; init; }
    }
}
