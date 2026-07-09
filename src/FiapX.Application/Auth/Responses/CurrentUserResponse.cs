namespace FiapX.Application.Auth.Responses;

public sealed record CurrentUserResponse
{
    public required string Id { get; set; }
    public string? Name { get; set; }
    public string? Email { get; set; }
}
