using AutVent.CorePlatform.Api.Common.Requests;
using AutVent.CorePlatform.Api.Common.Responses;
using AutVent.CorePlatform.Api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AutVent.CorePlatform.Api.Controllers;

[Route("api/[controller]")]
[ApiController]
public class ReferralController(IReferralService referralService) : ApiControllerBase
{
    [AllowAnonymous]
    [HttpGet("validate")]
    [ProducesResponseType(typeof(ApiResponse<ValidateReferralCodeResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<ValidateReferralCodeResponse>), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> ValidateReferralCode([FromQuery] string referralCode, CancellationToken cancellationToken)
    {
        var response = await referralService.ValidateReferralCodeAsync(referralCode, cancellationToken);
        return StatusCode(response.StatusCode, response);
    }

    [Authorize]
    [HttpGet("businesses/{referrerId:long}")]
    [ProducesResponseType(typeof(ApiResponse<PagedResponse<ReferredBusinessResponse>>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<PagedResponse<ReferredBusinessResponse>>), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ApiResponse<PagedResponse<ReferredBusinessResponse>>), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetReferredBusinesses(long referrerId, [FromQuery] PagedQueryRequest request, CancellationToken cancellationToken)
    {
        var response = await referralService.GetReferredBusinessesAsync(referrerId, request, CurrentUserId, cancellationToken);
        return StatusCode(response.StatusCode, response);
    }
}
