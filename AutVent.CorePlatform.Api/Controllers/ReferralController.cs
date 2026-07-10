using AutVent.CorePlatform.Api.Common.Responses;
using AutVent.CorePlatform.Api.Services;
using Microsoft.AspNetCore.Mvc;

namespace AutVent.CorePlatform.Api.Controllers;

[Route("api/[controller]")]
[ApiController]
public class ReferralController(IReferralService referralService) : ControllerBase
{
    [HttpGet("validate")]
    [ProducesResponseType(typeof(ApiResponse<ValidateReferralCodeResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<ValidateReferralCodeResponse>), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> ValidateReferralCode([FromQuery] string referralCode, CancellationToken cancellationToken)
    {
        var response = await referralService.ValidateReferralCodeAsync(referralCode, cancellationToken);
        return StatusCode(response.StatusCode, response);
    }
}
