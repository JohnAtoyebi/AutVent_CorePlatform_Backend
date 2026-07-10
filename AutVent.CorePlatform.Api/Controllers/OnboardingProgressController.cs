using AutVent.CorePlatform.Api.Common.Responses;
using AutVent.CorePlatform.Api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AutVent.CorePlatform.Api.Controllers;

[Route("api/onboarding")]
[Authorize]
public class OnboardingProgressController(IOnboardingProgressService progressService) : ApiControllerBase
{
    [HttpGet("progress")]
    [ProducesResponseType(typeof(ApiResponse<OnboardingProgressResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<OnboardingProgressResponse>), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetProgress(CancellationToken cancellationToken)
    {
        var response = await progressService.GetProgressAsync(CurrentUserId, cancellationToken);
        return StatusCode(response.StatusCode, response);
    }
}
