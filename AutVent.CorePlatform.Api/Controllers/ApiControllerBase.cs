using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using AutVent.CorePlatform.Api.Common.Responses;
using Microsoft.AspNetCore.Mvc;

namespace AutVent.CorePlatform.Api.Controllers;

[ApiController]
public abstract class ApiControllerBase : ControllerBase
{
    protected long CurrentUserId
    {
        get
        {
            var claim = User.FindFirstValue(JwtRegisteredClaimNames.Sub)
                ?? User.FindFirstValue(ClaimTypes.NameIdentifier);

            if (string.IsNullOrWhiteSpace(claim) || !long.TryParse(claim, out var userId))
            {
                throw new UnauthorizedAccessException("User identity could not be resolved from token.");
            }

            return userId;
        }
    }

    protected IActionResult Unauthorized<T>(string message) =>
        StatusCode(StatusCodes.Status401Unauthorized,
            ApiResponse<T>.Failed(StatusCodes.Status401Unauthorized, message));
}
