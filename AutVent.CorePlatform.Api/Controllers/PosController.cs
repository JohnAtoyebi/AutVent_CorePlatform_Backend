using AutVent.CorePlatform.Api.Common.Requests;
using AutVent.CorePlatform.Api.Common.Responses;
using AutVent.CorePlatform.Api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AutVent.CorePlatform.Api.Controllers;

[Route("api/[controller]")]
[Authorize]
public class PosController(IPosService posService) : ApiControllerBase
{
    [HttpPost("store/{storeId:long}/checkout")]
    [ProducesResponseType(typeof(ApiResponse<SaleResponse>), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ApiResponse<SaleResponse>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse<SaleResponse>), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ApiResponse<SaleResponse>), StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Checkout(long storeId, [FromBody] CreateSaleRequest request, CancellationToken cancellationToken)
    {
        var response = await posService.CreateSaleAsync(request, CurrentUserId, storeId, cancellationToken);
        return StatusCode(response.StatusCode, response);
    }

    [HttpGet("sale/{id:long}")]
    [ProducesResponseType(typeof(ApiResponse<SaleResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<SaleResponse>), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ApiResponse<SaleResponse>), StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> GetSaleById(long id, CancellationToken cancellationToken)
    {
        var response = await posService.GetSaleByIdAsync(id, CurrentUserId, cancellationToken);
        return StatusCode(response.StatusCode, response);
    }

    [HttpGet("store/{storeId:long}/sales")]
    [ProducesResponseType(typeof(ApiResponse<PagedResponse<SaleResponse>>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<PagedResponse<SaleResponse>>), StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> GetSalesByStore(long storeId, [FromQuery] PagedQueryRequest request, CancellationToken cancellationToken)
    {
        var response = await posService.GetSalesByStoreAsync(request, CurrentUserId, storeId, cancellationToken);
        return StatusCode(response.StatusCode, response);
    }

    [HttpGet("sales")]
    [ProducesResponseType(typeof(ApiResponse<PagedResponse<SaleResponse>>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAllSales([FromQuery] PagedQueryRequest request, CancellationToken cancellationToken)
    {
        var response = await posService.GetAllSalesAsync(request, CurrentUserId, cancellationToken);
        return StatusCode(response.StatusCode, response);
    }
}
