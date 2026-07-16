using AutVent.CorePlatform.Api.Common.Requests;
using AutVent.CorePlatform.Api.Common.Responses;
using AutVent.CorePlatform.Api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AutVent.CorePlatform.Api.Controllers;

[Route("api/[controller]")]
[Authorize]
public class UserController(IUserService userService) : ApiControllerBase
{
    [HttpGet("me")]
    [ProducesResponseType(typeof(ApiResponse<UserProfileResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<UserProfileResponse>), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetCurrentUser(CancellationToken cancellationToken)
    {
        var response = await userService.GetCurrentUserAsync(CurrentUserId, cancellationToken);
        return StatusCode(response.StatusCode, response);
    }

    [HttpPut("me")]
    [ProducesResponseType(typeof(ApiResponse<UpdateProfileResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<UpdateProfileResponse>), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ApiResponse<UpdateProfileResponse>), StatusCodes.Status409Conflict)]
    public async Task<IActionResult> UpdateProfile([FromBody] UpdateProfileRequest request, CancellationToken cancellationToken)
    {
        var response = await userService.UpdateProfileAsync(CurrentUserId, request, cancellationToken);
        return StatusCode(response.StatusCode, response);
    }

    [HttpPut("me/change-password")]
    [ProducesResponseType(typeof(ApiResponse<ChangePasswordResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<ChangePasswordResponse>), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ApiResponse<ChangePasswordResponse>), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> ChangePassword([FromBody] ChangePasswordRequest request, CancellationToken cancellationToken)
    {
        var response = await userService.ChangePasswordAsync(CurrentUserId, request, cancellationToken);
        return StatusCode(response.StatusCode, response);
    }

    [HttpPut("me/change-email")]
    [ProducesResponseType(typeof(ApiResponse<ChangeEmailResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<ChangeEmailResponse>), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ApiResponse<ChangeEmailResponse>), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ApiResponse<ChangeEmailResponse>), StatusCodes.Status409Conflict)]
    public async Task<IActionResult> ChangeEmail([FromBody] ChangeEmailRequest request, CancellationToken cancellationToken)
    {
        var response = await userService.ChangeEmailAsync(CurrentUserId, request, cancellationToken);
        return StatusCode(response.StatusCode, response);
    }
}

