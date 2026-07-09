namespace FiapX.Application.Abstractions.Auth;

public interface IUserProfileService
{
    Task<UserProfile> GetCurrentUserAsync(CancellationToken cancellationToken);
}

public sealed record UserProfile(
    string Id,
    string? Name,
    string? Email);
