using AutVent.CorePlatform.Api.Common.Requests;
using AutVent.CorePlatform.Api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AutVent.CorePlatform.Api.Controllers;

[Authorize]
[Route("api/billing")]
public sealed class BillingController(IBillingService billingService) : ApiControllerBase
{
    [HttpPost("transactions")]
    [ProducesResponseType(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Create(
        [FromBody] CreateBillingTransactionRequest request,
        CancellationToken cancellationToken = default)
    {
        var result = await billingService.CreateAsync(CurrentUserId, request, cancellationToken);
        return StatusCode(result.StatusCode, result);
    }

    [HttpPut("transactions/verify")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Verify(
        [FromBody] VerifyBillingTransactionRequest request,
        CancellationToken cancellationToken = default)
    {
        var result = await billingService.VerifyAsync(request, cancellationToken);
        return StatusCode(result.StatusCode, result);
    }

    [HttpGet("transactions/{id:long}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetById(
        long id,
        CancellationToken cancellationToken = default)
    {
        var result = await billingService.GetByIdAsync(id, cancellationToken);
        return StatusCode(result.StatusCode, result);
    }

    [HttpGet("transactions")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAll(
        [FromQuery] PagedQueryRequest request,
        CancellationToken cancellationToken = default)
    {
        var result = await billingService.GetAllAsync(CurrentUserId, request, cancellationToken);
        return StatusCode(result.StatusCode, result);
    }
}
