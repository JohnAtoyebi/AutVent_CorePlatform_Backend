using AutVent.CorePlatform.Api.Common.Requests;
using AutVent.CorePlatform.Api.Common.Responses;
using AutVent.CorePlatform.Api.Services;
using Microsoft.AspNetCore.Mvc;

namespace AutVent.CorePlatform.Api.Controllers;

[Route("api/[controller]")]
[ApiController]
public class AuthenticationController(IAuthenticationService authenticationService) : ControllerBase
{
    [HttpPost("sign-in")]
    [ProducesResponseType(typeof(ApiResponse<SignInResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<SignInResponse>), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ApiResponse<SignInResponse>), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ApiResponse<SignInResponse>), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> SignIn([FromBody] SignInRequest request, CancellationToken cancellationToken)
    {
        var response = await authenticationService.SignInAsync(request, cancellationToken);
        return StatusCode(response.StatusCode, response);
    }

    [HttpPost("reset-password")]
    [ProducesResponseType(typeof(ApiResponse<ResetPasswordResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<ResetPasswordResponse>), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ApiResponse<ResetPasswordResponse>), StatusCodes.Status409Conflict)]
    public async Task<IActionResult> ResetPassword([FromBody] ResetPasswordRequest request, CancellationToken cancellationToken)
    {
        var response = await authenticationService.ResetPasswordAsync(request, cancellationToken);
        return StatusCode(response.StatusCode, response);
    }
}
