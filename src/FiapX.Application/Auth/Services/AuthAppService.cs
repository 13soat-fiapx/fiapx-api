using FiapX.Application.Abstractions.Auth;

namespace FiapX.Application.Auth.Services;

public sealed class AuthAppService(IUserProfileService userProfileService)
{
    public Task<UserProfile> GetCurrentUserAsync(CancellationToken cancellationToken) =>
        userProfileService.GetCurrentUserAsync(cancellationToken);
}
