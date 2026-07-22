using AutVent.CorePlatform.Api.Common.Requests;
using AutVent.CorePlatform.Api.Common.Responses;
using AutVent.CorePlatform.Api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AutVent.CorePlatform.Api.Controllers;

[Route("api/[controller]")]
public class WaitlistController(IWaitlistService waitlistService) : ApiControllerBase
{
    /// <summary>Join the waitlist. No authentication required.</summary>
    [HttpPost]
    [AllowAnonymous]
    [ProducesResponseType(typeof(ApiResponse<WaitlistResponse>), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ApiResponse<WaitlistResponse>), StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Join([FromBody] JoinWaitlistRequest request, CancellationToken cancellationToken)
    {
        var response = await waitlistService.JoinAsync(request, cancellationToken);
        return StatusCode(response.StatusCode, response);
    }

    /// <summary>List all waitlist entries. Admin use.</summary>
    [HttpGet]
    [Authorize]
    [ProducesResponseType(typeof(ApiResponse<PagedResponse<WaitlistResponse>>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAll([FromQuery] PagedQueryRequest request, CancellationToken cancellationToken)
    {
        var response = await waitlistService.GetAllAsync(request, cancellationToken);
        return StatusCode(response.StatusCode, response);
    }

    /// <summary>Mark a waitlist entry as contacted.</summary>
    [HttpPatch("{id:long}/contacted")]
    [Authorize]
    [ProducesResponseType(typeof(ApiResponse<bool>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<bool>), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> MarkContacted(long id, CancellationToken cancellationToken)
    {
        var response = await waitlistService.MarkContactedAsync(id, cancellationToken);
        return StatusCode(response.StatusCode, response);
    }

    /// <summary>Remove a waitlist entry.</summary>
    [HttpDelete("{id:long}")]
    [Authorize]
    [ProducesResponseType(typeof(ApiResponse<bool>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<bool>), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete(long id, CancellationToken cancellationToken)
    {
        var response = await waitlistService.DeleteAsync(id, cancellationToken);
        return StatusCode(response.StatusCode, response);
    }
}
