using FiapX.Application.Auth.Responses;
using FiapX.Application.Auth.Services;
using Microsoft.AspNetCore.Mvc;

namespace FiapX.Api.Controllers;

[ApiController]
[Produces("application/json")]
[Route("v1/me")]
public sealed class AuthController(AuthAppService authAppService) : ControllerBase
{
    [HttpGet("")]
    [ProducesResponseType(typeof(CurrentUserResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> GetCurrentUserAsync(CancellationToken cancellationToken)
    {
        var profile = await authAppService.GetCurrentUserAsync(cancellationToken);

        return Ok(new CurrentUserResponse
        {
            Id = profile.Id,
            Name = profile.Name,
            Email = profile.Email
        });
    }
}
