using System.ComponentModel.DataAnnotations;
using System.Reflection;
using AutVent.CorePlatform.Api.Common.Requests;
using AutVent.CorePlatform.Api.Common.Responses;
using AutVent.CorePlatform.Api.Services;
using AutVent.CorePlatform.Domain.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AutVent.CorePlatform.Api.Controllers;

[Route("api/[controller]")]
[Authorize]
public class InventoryController(IInventoryService inventoryService) : ApiControllerBase
{
    [HttpGet("store/{storeId:long}/summary")]
    [ProducesResponseType(typeof(ApiResponse<InventorySummaryResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<InventorySummaryResponse>), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ApiResponse<InventorySummaryResponse>), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ApiResponse<InventorySummaryResponse>), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> GetSummary(long storeId, [FromQuery] InventorySummaryFilterRequest request, CancellationToken cancellationToken)
    {
        var response = await inventoryService.GetSummaryAsync(request, CurrentUserId, storeId, cancellationToken);
        return StatusCode(response.StatusCode, response);
    }

    [HttpGet("store/{storeId:long}/items")]
    [ProducesResponseType(typeof(ApiResponse<PagedResponse<InventoryItemResponse>>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<PagedResponse<InventoryItemResponse>>), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ApiResponse<PagedResponse<InventoryItemResponse>>), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetItems(long storeId, [FromQuery] PagedQueryRequest request, CancellationToken cancellationToken)
    {
        var response = await inventoryService.GetItemsAsync(request, CurrentUserId, storeId, cancellationToken);
        return StatusCode(response.StatusCode, response);
    }

    [HttpPatch("store/{storeId:long}/product/{productId:long}/stock")]
    [ProducesResponseType(typeof(ApiResponse<InventoryItemResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<InventoryItemResponse>), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ApiResponse<InventoryItemResponse>), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UpdateStock(long storeId, long productId, [FromBody] UpdateInventoryStockRequest request, CancellationToken cancellationToken)
    {
        var response = await inventoryService.UpdateStockAsync(productId, request, CurrentUserId, storeId, cancellationToken);
        return StatusCode(response.StatusCode, response);
    }

    [HttpGet("stock-adjustment-reasons")]
    [ProducesResponseType(typeof(ApiResponse<IEnumerable<EnumLookupResponse>>), StatusCodes.Status200OK)]
    public IActionResult GetStockAdjustmentReasons()
    {
        var reasons = Enum.GetValues<StockAdjustmentReason>()
            .Select(r =>
            {
                var memberInfo = typeof(StockAdjustmentReason).GetMember(r.ToString()).FirstOrDefault();
                var label = memberInfo?.GetCustomAttribute<DisplayAttribute>()?.Name ?? r.ToString();
                return new EnumLookupResponse { Value = (int)r, Label = label };
            });

        return Ok(ApiResponse<IEnumerable<EnumLookupResponse>>.Ok(reasons));
    }
}
