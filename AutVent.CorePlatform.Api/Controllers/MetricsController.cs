using AutVent.CorePlatform.Api.Common.Requests;
using AutVent.CorePlatform.Api.Common.Responses;
using AutVent.CorePlatform.Api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AutVent.CorePlatform.Api.Controllers;

[Route("api/[controller]")]
[Authorize]
public class MetricsController(IMetricsService metricsService) : ApiControllerBase
{
    [HttpGet]
    [ProducesResponseType(typeof(ApiResponse<MetricsResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<MetricsResponse>), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Get([FromQuery] MetricsRequest request, CancellationToken cancellationToken)
    {
        var response = await metricsService.GetAsync(request, CurrentUserId, cancellationToken);
        return StatusCode(response.StatusCode, response);
    }

    [HttpGet("sales-summary")]
    [ProducesResponseType(typeof(ApiResponse<SalesSummaryResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<SalesSummaryResponse>), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetSalesSummary([FromQuery] SalesSummaryRequest request, CancellationToken cancellationToken)
    {
        var response = await metricsService.GetSalesSummaryAsync(request, CurrentUserId, cancellationToken);
        return StatusCode(response.StatusCode, response);
    }

    [HttpGet("sales-graph")]
    [ProducesResponseType(typeof(ApiResponse<SalesGraphResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<SalesGraphResponse>), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetSalesGraph([FromQuery] SalesSummaryRequest request, CancellationToken cancellationToken)
    {
        var response = await metricsService.GetSalesGraphAsync(request, CurrentUserId, cancellationToken);
        return StatusCode(response.StatusCode, response);
    }

    [HttpGet("recent-transactions")]
    [ProducesResponseType(typeof(ApiResponse<PagedResponse<SaleResponse>>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<PagedResponse<SaleResponse>>), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetRecentTransactions(
        [FromQuery] SalesSummaryRequest request,
        [FromQuery] PagedQueryRequest paging,
        CancellationToken cancellationToken)
    {
        var response = await metricsService.GetRecentTransactionsAsync(request, paging, CurrentUserId, cancellationToken);
        return StatusCode(response.StatusCode, response);
    }
}
