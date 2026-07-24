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
    /// <summary>Paged notification feed for a specific store.</summary>
    [HttpGet("store/{storeId:long}")]
    [ProducesResponseType(typeof(ApiResponse<PagedResponse<NotificationResponse>>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetFeedByStore(long storeId, [FromQuery] NotificationFeedRequest request, CancellationToken cancellationToken)
    {
        var response = await notificationService.GetFeedAsync(CurrentUserId, storeId, request, cancellationToken);
        return StatusCode(response.StatusCode, response);
    }

    /// <summary>Paged notification feed for all users.</summary>
    [HttpGet]
    [ProducesResponseType(typeof(ApiResponse<PagedResponse<NotificationResponse>>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetFeed([FromQuery] NotificationFeedRequest request, CancellationToken cancellationToken)
    {
        var response = await notificationService.GetFeedAsync(CurrentUserId, request.StoreId, request, cancellationToken);
        return StatusCode(response.StatusCode, response);
    }

    /// <summary>Fast integer count for the badge on the navbar bell icon for a specific store.</summary>
    [HttpGet("store/{storeId:long}/unread-count")]
    [ProducesResponseType(typeof(ApiResponse<UnreadCountResponse>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetUnreadCountByStore(long storeId, CancellationToken cancellationToken)
    {
        var response = await notificationService.GetUnreadCountAsync(CurrentUserId, storeId, cancellationToken);
        return StatusCode(response.StatusCode, response);
    }

    /// <summary>Fast integer count for the badge on the navbar bell icon.</summary>
    [HttpGet("unread-count")]
    [ProducesResponseType(typeof(ApiResponse<UnreadCountResponse>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetUnreadCount([FromQuery] long? storeId, CancellationToken cancellationToken)
    {
        var response = await notificationService.GetUnreadCountAsync(CurrentUserId, storeId, cancellationToken);
        return StatusCode(response.StatusCode, response);
    }

    /// <summary>Mark a single notification as read when the user clicks it.</summary>
    [HttpPatch("store/{storeId:long}/{id:long}/read")]
    [ProducesResponseType(typeof(ApiResponse<bool>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<bool>), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> MarkRead(long storeId, long id, CancellationToken cancellationToken)
    {
        var response = await notificationService.MarkReadAsync(CurrentUserId, storeId, id, cancellationToken);
        return StatusCode(response.StatusCode, response);
    }

    /// <summary>Mark a single notification as read without store filtering.</summary>
    [HttpPatch("{id:long}/read")]
    [ProducesResponseType(typeof(ApiResponse<bool>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<bool>), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> MarkReadWithoutStore(long id, [FromQuery] long? storeId, CancellationToken cancellationToken)
    {
        var response = await notificationService.MarkReadAsync(CurrentUserId, storeId, id, cancellationToken);
        return StatusCode(response.StatusCode, response);
    }

    /// <summary>Mark all unread notifications as read for a specific store.</summary>
    [HttpPatch("store/{storeId:long}/read-all")]
    [ProducesResponseType(typeof(ApiResponse<bool>), StatusCodes.Status200OK)]
    public async Task<IActionResult> MarkAllReadByStore(long storeId, CancellationToken cancellationToken)
    {
        var response = await notificationService.MarkAllReadAsync(CurrentUserId, storeId, cancellationToken);
        return StatusCode(response.StatusCode, response);
    }

    /// <summary>Mark all unread notifications as read.</summary>
    [HttpPatch("read-all")]
    [ProducesResponseType(typeof(ApiResponse<bool>), StatusCodes.Status200OK)]
    public async Task<IActionResult> MarkAllRead([FromQuery] long? storeId, CancellationToken cancellationToken)
    {
        var response = await notificationService.MarkAllReadAsync(CurrentUserId, storeId, cancellationToken);
        return StatusCode(response.StatusCode, response);
    }

    /// <summary>Dismiss (soft-delete) a single notification from a specific store.</summary>
    [HttpDelete("store/{storeId:long}/{id:long}")]
    [ProducesResponseType(typeof(ApiResponse<bool>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<bool>), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete(long storeId, long id, CancellationToken cancellationToken)
    {
        var response = await notificationService.DeleteAsync(CurrentUserId, storeId, id, cancellationToken);
        return StatusCode(response.StatusCode, response);
    }

    /// <summary>Dismiss (soft-delete) a single notification.</summary>
    [HttpDelete("{id:long}")]
    [ProducesResponseType(typeof(ApiResponse<bool>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<bool>), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeleteWithoutStore(long id, [FromQuery] long? storeId, CancellationToken cancellationToken)
    {
        var response = await notificationService.DeleteAsync(CurrentUserId, storeId, id, cancellationToken);
        return StatusCode(response.StatusCode, response);
    }
}
