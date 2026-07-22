using AutVent.CorePlatform.Api.Common.Requests;
using AutVent.CorePlatform.Api.Common.Responses;
using AutVent.CorePlatform.Api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AutVent.CorePlatform.Api.Controllers;

[Route("api/[controller]")]
[Authorize]
public class AuditLogController(IAuditLogService auditLogService) : ApiControllerBase
{
    /// <summary>
    /// Returns a paged audit trail scoped to a business.
    /// Supports optional filters: action, entityType, from, to (via Filters dictionary).
    /// </summary>
    [HttpGet("business/{businessId:long}")]
    [ProducesResponseType(typeof(ApiResponse<PagedResponse<AuditLogResponse>>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetByBusiness(
        long businessId,
        [FromQuery] PagedQueryRequest request,
        CancellationToken cancellationToken)
    {
        var response = await auditLogService.GetAsync(businessId, null, request, cancellationToken);
        return StatusCode(response.StatusCode, response);
    }

    /// <summary>
    /// Returns a paged audit trail for the currently authenticated user.
    /// </summary>
    [HttpGet("me")]
    [ProducesResponseType(typeof(ApiResponse<PagedResponse<AuditLogResponse>>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetMine(
        [FromQuery] PagedQueryRequest request,
        CancellationToken cancellationToken)
    {
        var response = await auditLogService.GetAsync(null, CurrentUserId, request, cancellationToken);
        return StatusCode(response.StatusCode, response);
    }

    /// <summary>
    /// Returns a paged audit trail for a specific user (admin use).
    /// </summary>
    [HttpGet("user/{userId:long}")]
    [ProducesResponseType(typeof(ApiResponse<PagedResponse<AuditLogResponse>>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetByUser(
        long userId,
        [FromQuery] PagedQueryRequest request,
        CancellationToken cancellationToken)
    {
        var response = await auditLogService.GetAsync(null, userId, request, cancellationToken);
        return StatusCode(response.StatusCode, response);
    }
}
