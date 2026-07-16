using AutVent.CorePlatform.Api.Common.Requests;
using AutVent.CorePlatform.Api.Common.Responses;
using AutVent.CorePlatform.Api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AutVent.CorePlatform.Api.Controllers;

[Route("api/store/{storeId:long}/invoice")]
[Authorize]
public class InvoiceController(IInvoiceService invoiceService) : ApiControllerBase
{
    [HttpPost]
    [ProducesResponseType(typeof(ApiResponse<InvoiceResponse>), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ApiResponse<InvoiceResponse>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse<InvoiceResponse>), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Create(long storeId, [FromBody] CreateInvoiceRequest request, CancellationToken cancellationToken)
    {
        var response = await invoiceService.CreateAsync(storeId, CurrentUserId, request, cancellationToken);
        return StatusCode(response.StatusCode, response);
    }

    [HttpGet("{invoiceId:long}")]
    [ProducesResponseType(typeof(ApiResponse<InvoiceResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<InvoiceResponse>), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetById(long storeId, long invoiceId, CancellationToken cancellationToken)
    {
        var response = await invoiceService.GetByIdAsync(storeId, invoiceId, cancellationToken);
        return StatusCode(response.StatusCode, response);
    }

    [HttpGet]
    [ProducesResponseType(typeof(ApiResponse<PagedResponse<InvoiceResponse>>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAll(long storeId, [FromQuery] PagedQueryRequest request, CancellationToken cancellationToken)
    {
        var response = await invoiceService.GetAllAsync(storeId, request, cancellationToken);
        return StatusCode(response.StatusCode, response);
    }

    [HttpPut("{invoiceId:long}")]
    [ProducesResponseType(typeof(ApiResponse<InvoiceResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<InvoiceResponse>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse<InvoiceResponse>), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Update(long storeId, long invoiceId, [FromBody] UpdateInvoiceRequest request, CancellationToken cancellationToken)
    {
        var response = await invoiceService.UpdateAsync(storeId, invoiceId, CurrentUserId, request, cancellationToken);
        return StatusCode(response.StatusCode, response);
    }

    [HttpPatch("{invoiceId:long}/send")]
    [ProducesResponseType(typeof(ApiResponse<InvoiceResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<InvoiceResponse>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse<InvoiceResponse>), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> MarkAsSent(long storeId, long invoiceId, CancellationToken cancellationToken)
    {
        var response = await invoiceService.MarkAsSentAsync(storeId, invoiceId, cancellationToken);
        return StatusCode(response.StatusCode, response);
    }

    [HttpPatch("{invoiceId:long}/payment")]
    [ProducesResponseType(typeof(ApiResponse<InvoiceResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<InvoiceResponse>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse<InvoiceResponse>), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> RecordPayment(long storeId, long invoiceId, [FromBody] RecordInvoicePaymentRequest request, CancellationToken cancellationToken)
    {
        var response = await invoiceService.RecordPaymentAsync(storeId, invoiceId, request, cancellationToken);
        return StatusCode(response.StatusCode, response);
    }

    [HttpPatch("{invoiceId:long}/cancel")]
    [ProducesResponseType(typeof(ApiResponse<InvoiceResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<InvoiceResponse>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse<InvoiceResponse>), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Cancel(long storeId, long invoiceId, CancellationToken cancellationToken)
    {
        var response = await invoiceService.CancelAsync(storeId, invoiceId, cancellationToken);
        return StatusCode(response.StatusCode, response);
    }

    [HttpDelete("{invoiceId:long}")]
    [ProducesResponseType(typeof(ApiResponse<bool>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<bool>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse<bool>), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete(long storeId, long invoiceId, CancellationToken cancellationToken)
    {
        var response = await invoiceService.DeleteAsync(storeId, invoiceId, cancellationToken);
        return StatusCode(response.StatusCode, response);
    }
}
