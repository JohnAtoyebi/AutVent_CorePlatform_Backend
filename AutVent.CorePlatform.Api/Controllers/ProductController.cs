using AutVent.CorePlatform.Api.Common.Requests;
using AutVent.CorePlatform.Api.Common.Responses;
using AutVent.CorePlatform.Api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AutVent.CorePlatform.Api.Controllers;

[Route("api/[controller]")]
[Authorize]
public class ProductController(IProductService productService) : ApiControllerBase
{
    [HttpPost]
    [ProducesResponseType(typeof(ApiResponse<IReadOnlyCollection<ProductResponse>>), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ApiResponse<IReadOnlyCollection<ProductResponse>>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse<IReadOnlyCollection<ProductResponse>>), StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Create([FromBody] List<CreateProductRequest> requests, CancellationToken cancellationToken)
    {
        var response = await productService.CreateAsync(requests, cancellationToken);
        return StatusCode(response.StatusCode, response);
    }

    [HttpPost("import")]
    [ProducesResponseType(typeof(ApiResponse<ProductImportResponse>), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ApiResponse<ProductImportResponse>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse<ProductImportResponse>), StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Import(IFormFile file, CancellationToken cancellationToken)
    {
        var response = await productService.ImportAsync(file, cancellationToken);
        return StatusCode(response.StatusCode, response);
    }

    [HttpGet("{id:long}")]
    [ProducesResponseType(typeof(ApiResponse<ProductResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<ProductResponse>), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetById(long id, CancellationToken cancellationToken)
    {
        var response = await productService.GetByIdAsync(id, cancellationToken);
        return StatusCode(response.StatusCode, response);
    }

    [HttpGet]
    [ProducesResponseType(typeof(ApiResponse<PagedResponse<ProductResponse>>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAll([FromQuery] PagedQueryRequest request, CancellationToken cancellationToken)
    {
        var response = await productService.GetAllAsync(request, cancellationToken);
        return StatusCode(response.StatusCode, response);
    }
}
