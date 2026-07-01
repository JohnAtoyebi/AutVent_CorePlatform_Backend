using AutVent.CorePlatform.Api.Common.Requests;
using AutVent.CorePlatform.Api.Common.Responses;
using AutVent.CorePlatform.Api.Services;
using Microsoft.AspNetCore.Mvc;

namespace AutVent.CorePlatform.Api.Controllers;

[Route("api/categories")]
[ApiController]
public class CategoryController(ICategoryService categoryService) : ControllerBase
{
    [HttpGet("business-industries")]
    [ProducesResponseType(typeof(ApiResponse<PagedResponse<CategoryResponse>>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetBusinessIndustries([FromQuery] PagedQueryRequest request, CancellationToken cancellationToken)
    {
        var response = await categoryService.GetBusinessIndustriesAsync(request, cancellationToken);
        return StatusCode(response.StatusCode, response);
    }

    [HttpPost("business-industries")]
    [ProducesResponseType(typeof(ApiResponse<CategoryResponse>), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ApiResponse<CategoryResponse>), StatusCodes.Status409Conflict)]
    public async Task<IActionResult> CreateBusinessIndustry([FromBody] CreateCategoryRequest request, CancellationToken cancellationToken)
    {
        var response = await categoryService.CreateBusinessIndustryAsync(request, cancellationToken);
        return StatusCode(response.StatusCode, response);
    }

    [HttpGet("product-categories")]
    [ProducesResponseType(typeof(ApiResponse<PagedResponse<CategoryResponse>>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetProductCategories([FromQuery] PagedQueryRequest request, CancellationToken cancellationToken)
    {
        var response = await categoryService.GetProductCategoriesAsync(request, cancellationToken);
        return StatusCode(response.StatusCode, response);
    }

    [HttpPost("product-categories")]
    [ProducesResponseType(typeof(ApiResponse<CategoryResponse>), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ApiResponse<CategoryResponse>), StatusCodes.Status409Conflict)]
    public async Task<IActionResult> CreateProductCategory([FromBody] CreateCategoryRequest request, CancellationToken cancellationToken)
    {
        var response = await categoryService.CreateProductCategoryAsync(request, cancellationToken);
        return StatusCode(response.StatusCode, response);
    }

    [HttpGet("store-categories")]
    [ProducesResponseType(typeof(ApiResponse<PagedResponse<CategoryResponse>>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetStoreCategories([FromQuery] PagedQueryRequest request, CancellationToken cancellationToken)
    {
        var response = await categoryService.GetStoreCategoriesAsync(request, cancellationToken);
        return StatusCode(response.StatusCode, response);
    }

    [HttpPost("store-categories")]
    [ProducesResponseType(typeof(ApiResponse<CategoryResponse>), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ApiResponse<CategoryResponse>), StatusCodes.Status409Conflict)]
    public async Task<IActionResult> CreateStoreCategory([FromBody] CreateCategoryRequest request, CancellationToken cancellationToken)
    {
        var response = await categoryService.CreateStoreCategoryAsync(request, cancellationToken);
        return StatusCode(response.StatusCode, response);
    }
}
