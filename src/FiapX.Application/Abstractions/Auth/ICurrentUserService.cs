namespace FiapX.Application.Abstractions.Auth;

public interface ICurrentUserService
{
    string UserId { get; }
    string UserName { get; }
    string UserEmail { get; }
}
