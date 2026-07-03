using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.IdentityModel.Tokens;

namespace FiapX.Tests.Integration.Helpers;

public sealed class TestTokenGenerator
{
    private static readonly SymmetricSecurityKey Key =
        new(Encoding.UTF8.GetBytes("fiapx-test-signing-key-must-be-32chars!"));

    public string GenerateToken(string userId = "auth0|test-user")
    {
        var claims = new List<Claim>
        {
            new("sub", userId),
            new(ClaimTypes.NameIdentifier, userId),
            new("name", "Test User"),
            new(ClaimTypes.Name, "Test User"),
            new("email", "test.user@example.com"),
            new(ClaimTypes.Email, "test.user@example.com")
        };

        var token = new JwtSecurityToken(
            claims: claims,
            expires: DateTime.UtcNow.AddMinutes(10),
            signingCredentials: new SigningCredentials(Key, SecurityAlgorithms.HmacSha256));

        return new JwtSecurityTokenHandler().WriteToken(token);
    }
}
