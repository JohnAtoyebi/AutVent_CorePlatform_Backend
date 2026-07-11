using AutVent.CorePlatform.Api.Common.Requests;
using AutVent.CorePlatform.Api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AutVent.CorePlatform.Api.Controllers;

[AllowAnonymous]
[Route("api/support")]
public sealed class SupportController(ISupportService supportService) : ApiControllerBase
{
    [HttpPost("contact")]
    public async Task<IActionResult> Contact(
        [FromBody] ContactSupportRequest request,
        CancellationToken cancellationToken = default)
    {
        var result = await supportService.ContactAsync(request, cancellationToken);
        return StatusCode(result.StatusCode, result);
    }
}
