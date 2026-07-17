using AutVent.CorePlatform.Api.Common.Requests;
using AutVent.CorePlatform.Api.Common.Responses;
using AutVent.CorePlatform.Api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AutVent.CorePlatform.Api.Controllers;

[Route("api/[controller]")]
[Authorize]
public class NotificationController(INotificationService notificationService) : ApiControllerBase
{
    /// <summary>Paged notification feed for the navbar bell dropdown.</summary>
    [HttpGet]
    [ProducesResponseType(typeof(ApiResponse<PagedResponse<NotificationResponse>>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetFeed([FromQuery] NotificationFeedRequest request, CancellationToken cancellationToken)
    {
        var response = await notificationService.GetFeedAsync(CurrentUserId, request, cancellationToken);
        return StatusCode(response.StatusCode, response);
    }

    /// <summary>Fast integer count for the badge on the navbar bell icon.</summary>
    [HttpGet("unread-count")]
    [ProducesResponseType(typeof(ApiResponse<UnreadCountResponse>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetUnreadCount(CancellationToken cancellationToken)
    {
        var response = await notificationService.GetUnreadCountAsync(CurrentUserId, cancellationToken);
        return StatusCode(response.StatusCode, response);
    }

    /// <summary>Mark a single notification as read when the user clicks it.</summary>
    [HttpPatch("{id:long}/read")]
    [ProducesResponseType(typeof(ApiResponse<bool>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<bool>), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> MarkRead(long id, CancellationToken cancellationToken)
    {
        var response = await notificationService.MarkReadAsync(CurrentUserId, id, cancellationToken);
        return StatusCode(response.StatusCode, response);
    }

    /// <summary>Mark all unread notifications as read ("Mark all as read" button).</summary>
    [HttpPatch("read-all")]
    [ProducesResponseType(typeof(ApiResponse<bool>), StatusCodes.Status200OK)]
    public async Task<IActionResult> MarkAllRead(CancellationToken cancellationToken)
    {
        var response = await notificationService.MarkAllReadAsync(CurrentUserId, cancellationToken);
        return StatusCode(response.StatusCode, response);
    }

    /// <summary>Dismiss (soft-delete) a single notification.</summary>
    [HttpDelete("{id:long}")]
    [ProducesResponseType(typeof(ApiResponse<bool>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<bool>), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete(long id, CancellationToken cancellationToken)
    {
        var response = await notificationService.DeleteAsync(CurrentUserId, id, cancellationToken);
        return StatusCode(response.StatusCode, response);
    }
}
