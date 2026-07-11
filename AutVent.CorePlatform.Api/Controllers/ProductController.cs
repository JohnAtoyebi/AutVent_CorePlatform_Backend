using AutVent.CorePlatform.Api.Common.Requests;
using AutVent.CorePlatform.Api.Common.Responses;
using AutVent.CorePlatform.Api.Services;
using AutVent.CorePlatform.Infrastructure.Persistence;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AutVent.CorePlatform.Api.Controllers;

[Route("api/[controller]")]
[Authorize]
public class ProductController(IProductService productService, IUnitOfWork unitOfWork) : ApiControllerBase
{
    [HttpPost("store/{storeId:long}")]
    [ProducesResponseType(typeof(ApiResponse<IReadOnlyCollection<ProductResponse>>), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ApiResponse<IReadOnlyCollection<ProductResponse>>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse<IReadOnlyCollection<ProductResponse>>), StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Create(long storeId, [FromBody] List<CreateProductRequest> requests, CancellationToken cancellationToken)
    {
        var response = await productService.CreateAsync(requests, CurrentUserId, storeId, cancellationToken);
        return StatusCode(response.StatusCode, response);
    }

    [HttpPost("store/{storeId:long}/import")]
    [ProducesResponseType(typeof(ApiResponse<ProductImportResponse>), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ApiResponse<ProductImportResponse>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse<ProductImportResponse>), StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Import(long storeId, IFormFile file, CancellationToken cancellationToken)
    {
        var response = await productService.ImportAsync(file, CurrentUserId, storeId, cancellationToken);
        return StatusCode(response.StatusCode, response);
    }

    [HttpGet("import-template")]
    [AllowAnonymous]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> GetImportTemplate(CancellationToken cancellationToken)
    {
        var stream = await ProductImportTemplateGenerator.GenerateTemplateAsync(unitOfWork);
        return File(stream, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", "ProductImportTemplate.xlsx");
    }

    [HttpGet("{id:long}")]
    [ProducesResponseType(typeof(ApiResponse<ProductResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<ProductResponse>), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ApiResponse<ProductResponse>), StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> GetById(long id, CancellationToken cancellationToken)
    {
        var response = await productService.GetByIdAsync(id, CurrentUserId, cancellationToken);
        return StatusCode(response.StatusCode, response);
    }

    [HttpGet]
    [ProducesResponseType(typeof(ApiResponse<PagedResponse<ProductResponse>>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAll([FromQuery] PagedQueryRequest request, CancellationToken cancellationToken)
    {
        var response = await productService.GetAllAsync(request, CurrentUserId, cancellationToken);
        return StatusCode(response.StatusCode, response);
    }

    [HttpGet("generate-sku")]
    [ProducesResponseType(typeof(ApiResponse<GenerateSkuResponse>), StatusCodes.Status200OK)]
    public IActionResult GenerateSku([FromQuery] string? productName, CancellationToken cancellationToken)
    {
        var response = productService.GenerateSku(productName ?? string.Empty);
        return StatusCode(response.StatusCode, response);
    }

    [HttpPatch("store/{storeId:long}/bulk-edit")]
    [ProducesResponseType(typeof(ApiResponse<IReadOnlyCollection<ProductResponse>>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<IReadOnlyCollection<ProductResponse>>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse<IReadOnlyCollection<ProductResponse>>), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ApiResponse<IReadOnlyCollection<ProductResponse>>), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> BulkEdit(long storeId, [FromBody] BulkEditProductRequest request, CancellationToken cancellationToken)
    {
        var response = await productService.BulkEditAsync(request, CurrentUserId, storeId, cancellationToken);
        return StatusCode(response.StatusCode, response);
    }

    [HttpPut("store/{storeId:long}/{id:long}")]
    [ProducesResponseType(typeof(ApiResponse<IReadOnlyCollection<ProductResponse>>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<IReadOnlyCollection<ProductResponse>>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse<IReadOnlyCollection<ProductResponse>>), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ApiResponse<IReadOnlyCollection<ProductResponse>>), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ApiResponse<IReadOnlyCollection<ProductResponse>>), StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Update(long storeId, long id, [FromBody] CreateProductRequest request, CancellationToken cancellationToken)
    {
        var response = await productService.UpdateAsync(id, request, CurrentUserId, storeId, cancellationToken);
        return StatusCode(response.StatusCode, response);
    }

    [HttpDelete("{id:long}")]
    [ProducesResponseType(typeof(ApiResponse<bool>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<bool>), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ApiResponse<bool>), StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> Delete(long id, CancellationToken cancellationToken)
    {
        var response = await productService.DeleteAsync(id, CurrentUserId, cancellationToken);
        return StatusCode(response.StatusCode, response);
    }

    [HttpPatch("{id:long}/status")]
    [ProducesResponseType(typeof(ApiResponse<bool>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<bool>), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ApiResponse<bool>), StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> UpdateStatus(long id, [FromBody] UpdateProductStatusRequest request, CancellationToken cancellationToken)
    {
        var response = await productService.UpdateStatusAsync(id, request.IsActive, CurrentUserId, cancellationToken);
        return StatusCode(response.StatusCode, response);
    }

    [HttpPost("{id:long}/images")]
    [RequestSizeLimit(50 * 1024 * 1024)]
    [ProducesResponseType(typeof(ApiResponse<ProductResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<ProductResponse>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse<ProductResponse>), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ApiResponse<ProductResponse>), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UploadImages(long id, [FromForm] List<IFormFile> files, CancellationToken cancellationToken)
    {
        var response = await productService.UploadImagesAsync(id, files, CurrentUserId, cancellationToken);
        return StatusCode(response.StatusCode, response);
    }

    [HttpDelete("{id:long}/images")]
    [ProducesResponseType(typeof(ApiResponse<ProductResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<ProductResponse>), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ApiResponse<ProductResponse>), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeleteImage(long id, [FromQuery] string imageUrl, CancellationToken cancellationToken)
    {
        var response = await productService.DeleteImageAsync(id, imageUrl, CurrentUserId, cancellationToken);
        return StatusCode(response.StatusCode, response);
    }
}
