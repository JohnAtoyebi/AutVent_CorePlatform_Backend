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

    [HttpGet("products")]
    [ProducesResponseType(typeof(ApiResponse<ProductMetricsResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<ProductMetricsResponse>), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetProductMetrics([FromQuery] MetricsRequest request, CancellationToken cancellationToken)
    {
        var response = await metricsService.GetProductMetricsAsync(request, CurrentUserId, cancellationToken);
        return StatusCode(response.StatusCode, response);
    }

    [HttpGet("inventory")]
    [ProducesResponseType(typeof(ApiResponse<InventoryMetricsResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<InventoryMetricsResponse>), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetInventoryMetrics([FromQuery] MetricsRequest request, CancellationToken cancellationToken)
    {
        var response = await metricsService.GetInventoryMetricsAsync(request, CurrentUserId, cancellationToken);
        return StatusCode(response.StatusCode, response);
    }

    [HttpGet("customers")]
    [ProducesResponseType(typeof(ApiResponse<CustomerMetricsResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<CustomerMetricsResponse>), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetCustomerMetrics([FromQuery] MetricsRequest request, CancellationToken cancellationToken)
    {
        var response = await metricsService.GetCustomerMetricsAsync(request, CurrentUserId, cancellationToken);
        return StatusCode(response.StatusCode, response);
    }

    [HttpGet("recent-transactions/{id:long}")]
    [ProducesResponseType(typeof(ApiResponse<SaleResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<SaleResponse>), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetTransactionById(long id, CancellationToken cancellationToken)
    {
        var response = await metricsService.GetTransactionByIdAsync(id, CurrentUserId, cancellationToken);
        return StatusCode(response.StatusCode, response);
    }

    [HttpGet("sales-by-location")]
    [ProducesResponseType(typeof(ApiResponse<SalesByLocationResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<SalesByLocationResponse>), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetSalesByLocation([FromQuery] MetricsRequest request, CancellationToken cancellationToken)
    {
        var response = await metricsService.GetSalesByLocationAsync(request, CurrentUserId, cancellationToken);
        return StatusCode(response.StatusCode, response);
    }

    [HttpGet("sales-by-category")]
    [ProducesResponseType(typeof(ApiResponse<SalesByCategoryResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<SalesByCategoryResponse>), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetSalesByCategory([FromQuery] MetricsRequest request, CancellationToken cancellationToken)
    {
        var response = await metricsService.GetSalesByCategoryAsync(request, CurrentUserId, cancellationToken);
        return StatusCode(response.StatusCode, response);
    }

    [HttpGet("payment-methods")]
    [ProducesResponseType(typeof(ApiResponse<PaymentMethodBreakdownResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<PaymentMethodBreakdownResponse>), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetPaymentMethodBreakdown([FromQuery] MetricsRequest request, CancellationToken cancellationToken)
    {
        var response = await metricsService.GetPaymentMethodBreakdownAsync(request, CurrentUserId, cancellationToken);
        return StatusCode(response.StatusCode, response);
    }

    [HttpGet("top-customers")]
    [ProducesResponseType(typeof(ApiResponse<TopCustomersResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<TopCustomersResponse>), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetTopCustomers([FromQuery] MetricsRequest request, CancellationToken cancellationToken)
    {
        var response = await metricsService.GetTopCustomersAsync(request, CurrentUserId, cancellationToken);
        return StatusCode(response.StatusCode, response);
    }

    [HttpGet("customer-growth")]
    [ProducesResponseType(typeof(ApiResponse<CustomerGrowthResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<CustomerGrowthResponse>), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetCustomerGrowth([FromQuery] MetricsRequest request, CancellationToken cancellationToken)
    {
        var response = await metricsService.GetCustomerGrowthAsync(request, CurrentUserId, cancellationToken);
        return StatusCode(response.StatusCode, response);
    }

    [HttpGet("staff")]
    [ProducesResponseType(typeof(ApiResponse<StaffAnalyticsResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<StaffAnalyticsResponse>), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetStaffAnalytics([FromQuery] MetricsRequest request, CancellationToken cancellationToken)
    {
        var response = await metricsService.GetStaffAnalyticsAsync(request, CurrentUserId, cancellationToken);
        return StatusCode(response.StatusCode, response);
    }

    [HttpGet("product-performance")]
    [ProducesResponseType(typeof(ApiResponse<ProductPerformanceResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<ProductPerformanceResponse>), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetProductPerformance([FromQuery] MetricsRequest request, CancellationToken cancellationToken)
    {
        var response = await metricsService.GetProductPerformanceAsync(request, CurrentUserId, cancellationToken);
        return StatusCode(response.StatusCode, response);
    }

    [HttpGet("financial")]
    [ProducesResponseType(typeof(ApiResponse<FinancialMetricsResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<FinancialMetricsResponse>), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetFinancialMetrics([FromQuery] MetricsRequest request, CancellationToken cancellationToken)
    {
        var response = await metricsService.GetFinancialMetricsAsync(request, CurrentUserId, cancellationToken);
        return StatusCode(response.StatusCode, response);
    }
}
