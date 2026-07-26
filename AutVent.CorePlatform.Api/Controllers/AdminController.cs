using System.ComponentModel.DataAnnotations;
using System.Reflection;
using AutVent.CorePlatform.Api.Common.Requests;
using AutVent.CorePlatform.Api.Common.Responses;
using AutVent.CorePlatform.Api.Services;
using AutVent.CorePlatform.Domain.Enums;
using AutVent.CorePlatform.Infrastructure.Persistence;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AutVent.CorePlatform.Api.Controllers;

[Route("api/admin")]
public sealed class AdminController(
    IAuthenticationService authenticationService,
    IBusinessService businessService,
    IBillingService billingService,
    IUserService userService,
    IStoreService storeService,
    ICategoryService categoryService,
    IProductService productService,
    IProductCategoryService productCategoryService,
    IInventoryService inventoryService,
    IStockTransferService stockTransferService,
    IPosService posService,
    IInvoiceService invoiceService,
    ICustomerService customerService,
    ISupplierService supplierService,
    IBankAccountService bankAccountService,
    IMetricsService metricsService,
    IAuditLogService auditLogService,
    INotificationService notificationService,
    ISupportService supportService,
    IWaitlistService waitlistService,
    IOnboardingProgressService onboardingProgressService,
    IUnitOfWork unitOfWork) : ApiControllerBase
{
    [AllowAnonymous]
    [HttpPost("authentication/sign-in")]
    [ProducesResponseType(typeof(ApiResponse<SignInResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<SignInResponse>), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ApiResponse<SignInResponse>), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ApiResponse<SignInResponse>), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> SignIn([FromBody] SignInRequest request, CancellationToken cancellationToken)
    {
        var response = await authenticationService.SignInAsync(request, cancellationToken);
        return StatusCode(response.StatusCode, response);
    }

    [AllowAnonymous]
    [HttpPost("authentication/send-reset-link")]
    [ProducesResponseType(typeof(ApiResponse<ForgotPasswordResponse>), StatusCodes.Status200OK)]
    public async Task<IActionResult> SendResetLink([FromBody] ForgotPasswordRequest request, CancellationToken cancellationToken)
    {
        var response = await authenticationService.SendResetLinkAsync(request, cancellationToken);
        return StatusCode(response.StatusCode, response);
    }

    [AllowAnonymous]
    [HttpPost("authentication/resend-reset-link")]
    [ProducesResponseType(typeof(ApiResponse<ForgotPasswordResponse>), StatusCodes.Status200OK)]
    public async Task<IActionResult> ResendResetLink([FromBody] ForgotPasswordRequest request, CancellationToken cancellationToken)
    {
        var response = await authenticationService.ResendResetLinkAsync(request, cancellationToken);
        return StatusCode(response.StatusCode, response);
    }

    [AllowAnonymous]
    [HttpPost("authentication/reset-password")]
    [ProducesResponseType(typeof(ApiResponse<ResetPasswordResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<ResetPasswordResponse>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse<ResetPasswordResponse>), StatusCodes.Status409Conflict)]
    public async Task<IActionResult> ResetPassword([FromBody] ResetPasswordRequest request, CancellationToken cancellationToken)
    {
        var response = await authenticationService.ResetPasswordAsync(request, cancellationToken);
        return StatusCode(response.StatusCode, response);
    }

    [AllowAnonymous]
    [HttpPost("authentication/refresh-token")]
    [ProducesResponseType(typeof(ApiResponse<RefreshTokenResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<RefreshTokenResponse>), StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> RefreshToken([FromBody] RefreshTokenRequest request, CancellationToken cancellationToken)
    {
        var response = await authenticationService.RefreshTokenAsync(request, cancellationToken);
        return StatusCode(response.StatusCode, response);
    }

    [Authorize]
    [HttpPost("business")]
    [ProducesResponseType(typeof(ApiResponse<CreateBusinessResponse>), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ApiResponse<CreateBusinessResponse>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse<CreateBusinessResponse>), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ApiResponse<CreateBusinessResponse>), StatusCodes.Status409Conflict)]
    public async Task<IActionResult> CreateBusiness([FromBody] CreateBusinessRequest request, CancellationToken cancellationToken)
    {
        var response = await businessService.CreateAsync(request, CurrentUserId, cancellationToken);
        return StatusCode(response.StatusCode, response);
    }

    [Authorize]
    [HttpGet("business")]
    [ProducesResponseType(typeof(ApiResponse<PagedResponse<CreateBusinessResponse>>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetBusinesses([FromQuery] PagedQueryRequest request, CancellationToken cancellationToken)
    {
        var response = await businessService.GetAllAsync(request, cancellationToken);
        return StatusCode(response.StatusCode, response);
    }

    [Authorize]
    [HttpGet("business/{id:long}")]
    [ProducesResponseType(typeof(ApiResponse<CreateBusinessResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<CreateBusinessResponse>), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetBusinessById(long id, CancellationToken cancellationToken)
    {
        var response = await businessService.GetByIdAsync(id, cancellationToken);
        return StatusCode(response.StatusCode, response);
    }

    [Authorize]
    [HttpPut("business/{id:long}")]
    [ProducesResponseType(typeof(ApiResponse<CreateBusinessResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<CreateBusinessResponse>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse<CreateBusinessResponse>), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ApiResponse<CreateBusinessResponse>), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UpdateBusiness(long id, [FromBody] UpdateBusinessRequest request, CancellationToken cancellationToken)
    {
        var response = await businessService.UpdateAsync(id, request, CurrentUserId, cancellationToken);
        return StatusCode(response.StatusCode, response);
    }

    [Authorize]
    [HttpGet("billing/transactions")]
    [ProducesResponseType(typeof(ApiResponse<PagedResponse<BillingTransactionResponse>>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetBillingTransactions([FromQuery] PagedQueryRequest request, CancellationToken cancellationToken)
    {
        var response = await billingService.GetAllAsync(CurrentUserId, request, cancellationToken);
        return StatusCode(response.StatusCode, response);
    }

    [Authorize]
    [HttpGet("billing/transactions/{id:long}")]
    [ProducesResponseType(typeof(ApiResponse<BillingTransactionResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<BillingTransactionResponse>), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetBillingTransactionById(long id, CancellationToken cancellationToken)
    {
        var response = await billingService.GetByIdAsync(id, cancellationToken);
        return StatusCode(response.StatusCode, response);
    }

    [Authorize]
    [HttpPut("billing/transactions/verify")]
    [ProducesResponseType(typeof(ApiResponse<BillingTransactionResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<BillingTransactionResponse>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse<BillingTransactionResponse>), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ApiResponse<BillingTransactionResponse>), StatusCodes.Status409Conflict)]
    public async Task<IActionResult> VerifyBillingTransaction([FromBody] VerifyBillingTransactionRequest request, CancellationToken cancellationToken)
    {
        var response = await billingService.VerifyAsync(request, cancellationToken);
        return StatusCode(response.StatusCode, response);
    }

    [Authorize]
    [HttpGet("billing/businesses/{businessId:long}/subscriptions")]
    [ProducesResponseType(typeof(ApiResponse<PagedResponse<BusinessSubscriptionResponse>>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<PagedResponse<BusinessSubscriptionResponse>>), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ApiResponse<PagedResponse<BusinessSubscriptionResponse>>), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetBusinessSubscriptions(long businessId, [FromQuery] PagedQueryRequest request, CancellationToken cancellationToken)
    {
        var response = await billingService.GetSubscriptionsByBusinessIdAsync(businessId, CurrentUserId, request, cancellationToken);
        return StatusCode(response.StatusCode, response);
    }

    [Authorize]
    [HttpGet("billing/businesses/{businessId:long}/subscriptions/active")]
    [ProducesResponseType(typeof(ApiResponse<BusinessSubscriptionResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<BusinessSubscriptionResponse>), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ApiResponse<BusinessSubscriptionResponse>), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetActiveBusinessSubscription(long businessId, CancellationToken cancellationToken)
    {
        var response = await billingService.GetActiveSubscriptionByBusinessIdAsync(businessId, CurrentUserId, cancellationToken);
        return StatusCode(response.StatusCode, response);
    }

    [Authorize]
    [HttpGet("user/me")]
    [ProducesResponseType(typeof(ApiResponse<UserProfileResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<UserProfileResponse>), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetCurrentUser(CancellationToken cancellationToken)
    {
        var response = await userService.GetCurrentUserAsync(CurrentUserId, cancellationToken);
        return StatusCode(response.StatusCode, response);
    }

    [Authorize]
    [HttpPut("user/me")]
    [ProducesResponseType(typeof(ApiResponse<UpdateProfileResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<UpdateProfileResponse>), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ApiResponse<UpdateProfileResponse>), StatusCodes.Status409Conflict)]
    public async Task<IActionResult> UpdateProfile([FromBody] UpdateProfileRequest request, CancellationToken cancellationToken)
    {
        var response = await userService.UpdateProfileAsync(CurrentUserId, request, cancellationToken);
        return StatusCode(response.StatusCode, response);
    }

    [Authorize]
    [HttpPut("user/me/change-password")]
    [ProducesResponseType(typeof(ApiResponse<ChangePasswordResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<ChangePasswordResponse>), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ApiResponse<ChangePasswordResponse>), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> ChangePassword([FromBody] ChangePasswordRequest request, CancellationToken cancellationToken)
    {
        var response = await userService.ChangePasswordAsync(CurrentUserId, request, cancellationToken);
        return StatusCode(response.StatusCode, response);
    }

    [Authorize]
    [HttpPut("user/me/change-email")]
    [ProducesResponseType(typeof(ApiResponse<ChangeEmailResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<ChangeEmailResponse>), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ApiResponse<ChangeEmailResponse>), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ApiResponse<ChangeEmailResponse>), StatusCodes.Status409Conflict)]
    public async Task<IActionResult> ChangeEmail([FromBody] ChangeEmailRequest request, CancellationToken cancellationToken)
    {
        var response = await userService.ChangeEmailAsync(CurrentUserId, request, cancellationToken);
        return StatusCode(response.StatusCode, response);
    }

    [Authorize]
    [HttpPost("store")]
    [ProducesResponseType(typeof(ApiResponse<CreateStoreResponse>), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ApiResponse<CreateStoreResponse>), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ApiResponse<CreateStoreResponse>), StatusCodes.Status409Conflict)]
    public async Task<IActionResult> CreateStore([FromBody] CreateStoreRequest request, CancellationToken cancellationToken)
    {
        var response = await storeService.CreateAsync(request, CurrentUserId, cancellationToken);
        return StatusCode(response.StatusCode, response);
    }

    [Authorize]
    [HttpGet("store/{id:long}")]
    [ProducesResponseType(typeof(ApiResponse<CreateStoreResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<CreateStoreResponse>), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetStoreById(long id, CancellationToken cancellationToken)
    {
        var response = await storeService.GetByIdAsync(id, CurrentUserId, cancellationToken);
        return StatusCode(response.StatusCode, response);
    }

    [Authorize]
    [HttpGet("store")]
    [ProducesResponseType(typeof(ApiResponse<PagedResponse<CreateStoreResponse>>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetStores([FromQuery] PagedQueryRequest request, CancellationToken cancellationToken)
    {
        var response = await storeService.GetAllAsync(request, CurrentUserId, cancellationToken);
        return StatusCode(response.StatusCode, response);
    }

    [Authorize]
    [HttpPut("store/{id:long}")]
    [ProducesResponseType(typeof(ApiResponse<CreateStoreResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<CreateStoreResponse>), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ApiResponse<CreateStoreResponse>), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ApiResponse<CreateStoreResponse>), StatusCodes.Status409Conflict)]
    public async Task<IActionResult> UpdateStore(long id, [FromBody] UpdateStoreRequest request, CancellationToken cancellationToken)
    {
        var response = await storeService.UpdateAsync(id, request, CurrentUserId, cancellationToken);
        return StatusCode(response.StatusCode, response);
    }

    [Authorize]
    [HttpPatch("store/{id:long}/deactivate")]
    [ProducesResponseType(typeof(ApiResponse<bool>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<bool>), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ApiResponse<bool>), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ApiResponse<bool>), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> DeactivateStore(long id, CancellationToken cancellationToken)
    {
        var response = await storeService.DeactivateAsync(id, CurrentUserId, cancellationToken);
        return StatusCode(response.StatusCode, response);
    }

    [Authorize]
    [HttpGet("store-categories")]
    [ProducesResponseType(typeof(ApiResponse<PagedResponse<CategoryResponse>>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetStoreCategories([FromQuery] PagedQueryRequest request, CancellationToken cancellationToken)
    {
        var response = await categoryService.GetStoreCategoriesAsync(request, cancellationToken);
        return StatusCode(response.StatusCode, response);
    }

    [Authorize]
    [HttpPost("store-categories")]
    [ProducesResponseType(typeof(ApiResponse<CategoryResponse>), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ApiResponse<CategoryResponse>), StatusCodes.Status409Conflict)]
    public async Task<IActionResult> CreateStoreCategory([FromBody] CreateCategoryRequest request, CancellationToken cancellationToken)
    {
        var response = await categoryService.CreateStoreCategoryAsync(request, cancellationToken);
        return StatusCode(response.StatusCode, response);
    }

    [Authorize]
    [HttpPost("product/store/{storeId:long}")]
    [ProducesResponseType(typeof(ApiResponse<IReadOnlyCollection<ProductResponse>>), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ApiResponse<IReadOnlyCollection<ProductResponse>>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse<IReadOnlyCollection<ProductResponse>>), StatusCodes.Status409Conflict)]
    public async Task<IActionResult> CreateProducts(long storeId, [FromBody] List<CreateProductRequest> requests, CancellationToken cancellationToken)
    {
        var response = await productService.CreateAsync(requests, CurrentUserId, storeId, cancellationToken);
        return StatusCode(response.StatusCode, response);
    }

    [Authorize]
    [HttpPost("product/store/{storeId:long}/import")]
    [ProducesResponseType(typeof(ApiResponse<ProductImportResponse>), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ApiResponse<ProductImportResponse>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse<ProductImportResponse>), StatusCodes.Status409Conflict)]
    public async Task<IActionResult> ImportProducts(long storeId, IFormFile file, CancellationToken cancellationToken)
    {
        var response = await productService.ImportAsync(file, CurrentUserId, storeId, cancellationToken);
        return StatusCode(response.StatusCode, response);
    }

    [Authorize]
    [HttpGet("product/import-template")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> GetProductImportTemplate(CancellationToken cancellationToken)
    {
        var stream = await ProductImportTemplateGenerator.GenerateTemplateAsync(unitOfWork);
        return File(stream, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", "ProductImportTemplate.xlsx");
    }

    [Authorize]
    [HttpGet("product/{id:long}")]
    [ProducesResponseType(typeof(ApiResponse<ProductResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<ProductResponse>), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ApiResponse<ProductResponse>), StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> GetProductById(long id, CancellationToken cancellationToken)
    {
        var response = await productService.GetByIdAsync(id, CurrentUserId, cancellationToken);
        return StatusCode(response.StatusCode, response);
    }

    [Authorize]
    [HttpGet("product")]
    [ProducesResponseType(typeof(ApiResponse<PagedResponse<ProductResponse>>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetProducts([FromQuery] PagedQueryRequest request, CancellationToken cancellationToken)
    {
        var response = await productService.GetAllAsync(request, CurrentUserId, cancellationToken);
        return StatusCode(response.StatusCode, response);
    }

    [Authorize]
    [HttpGet("product/generate-sku")]
    [ProducesResponseType(typeof(ApiResponse<GenerateSkuResponse>), StatusCodes.Status200OK)]
    public IActionResult GenerateProductSku([FromQuery] string? productName, CancellationToken cancellationToken)
    {
        var response = productService.GenerateSku(productName ?? string.Empty);
        return StatusCode(response.StatusCode, response);
    }

    [Authorize]
    [HttpPatch("product/store/{storeId:long}/bulk-edit")]
    [ProducesResponseType(typeof(ApiResponse<IReadOnlyCollection<ProductResponse>>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<IReadOnlyCollection<ProductResponse>>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse<IReadOnlyCollection<ProductResponse>>), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ApiResponse<IReadOnlyCollection<ProductResponse>>), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> BulkEditProducts(long storeId, [FromBody] BulkEditProductRequest request, CancellationToken cancellationToken)
    {
        var response = await productService.BulkEditAsync(request, CurrentUserId, storeId, cancellationToken);
        return StatusCode(response.StatusCode, response);
    }

    [Authorize]
    [HttpPut("product/store/{storeId:long}/{id:long}")]
    [ProducesResponseType(typeof(ApiResponse<IReadOnlyCollection<ProductResponse>>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<IReadOnlyCollection<ProductResponse>>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse<IReadOnlyCollection<ProductResponse>>), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ApiResponse<IReadOnlyCollection<ProductResponse>>), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ApiResponse<IReadOnlyCollection<ProductResponse>>), StatusCodes.Status409Conflict)]
    public async Task<IActionResult> UpdateProduct(long storeId, long id, [FromBody] CreateProductRequest request, CancellationToken cancellationToken)
    {
        var response = await productService.UpdateAsync(id, request, CurrentUserId, storeId, cancellationToken);
        return StatusCode(response.StatusCode, response);
    }

    [Authorize]
    [HttpDelete("product/{id:long}")]
    [ProducesResponseType(typeof(ApiResponse<bool>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<bool>), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ApiResponse<bool>), StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> DeleteProduct(long id, CancellationToken cancellationToken)
    {
        var response = await productService.DeleteAsync(id, CurrentUserId, cancellationToken);
        return StatusCode(response.StatusCode, response);
    }

    [Authorize]
    [HttpPatch("product/{id:long}/status")]
    [ProducesResponseType(typeof(ApiResponse<bool>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<bool>), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ApiResponse<bool>), StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> UpdateProductStatus(long id, [FromBody] UpdateProductStatusRequest request, CancellationToken cancellationToken)
    {
        var response = await productService.UpdateStatusAsync(id, request.IsActive, CurrentUserId, cancellationToken);
        return StatusCode(response.StatusCode, response);
    }

    [Authorize]
    [HttpPost("product/{id:long}/images")]
    [RequestSizeLimit(50 * 1024 * 1024)]
    [ProducesResponseType(typeof(ApiResponse<ProductResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<ProductResponse>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse<ProductResponse>), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ApiResponse<ProductResponse>), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UploadProductImages(long id, [FromForm] List<IFormFile> files, CancellationToken cancellationToken)
    {
        var response = await productService.UploadImagesAsync(id, files, CurrentUserId, cancellationToken);
        return StatusCode(response.StatusCode, response);
    }

    [Authorize]
    [HttpDelete("product/{id:long}/images")]
    [ProducesResponseType(typeof(ApiResponse<ProductResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<ProductResponse>), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ApiResponse<ProductResponse>), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeleteProductImage(long id, [FromQuery] string imageUrl, CancellationToken cancellationToken)
    {
        var response = await productService.DeleteImageAsync(id, imageUrl, CurrentUserId, cancellationToken);
        return StatusCode(response.StatusCode, response);
    }

    [Authorize]
    [HttpGet("product-categories")]
    [ProducesResponseType(typeof(ApiResponse<PagedResponse<CategoryResponse>>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetProductCategories([FromQuery] PagedQueryRequest request, CancellationToken cancellationToken)
    {
        var response = await productCategoryService.GetAllAsync(request, CurrentUserId, cancellationToken);
        return StatusCode(response.StatusCode, response);
    }

    [Authorize]
    [HttpPost("product-categories/batch")]
    [ProducesResponseType(typeof(ApiResponse<IList<CategoryResponse>>), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ApiResponse<IList<CategoryResponse>>), StatusCodes.Status207MultiStatus)]
    public async Task<IActionResult> CreateProductCategoriesBatch([FromBody] CreateCategoriesRequest request, CancellationToken cancellationToken)
    {
        var response = await productCategoryService.CreateBatchAsync(request, CurrentUserId, cancellationToken);
        return StatusCode(response.StatusCode, response);
    }

    [Authorize]
    [HttpDelete("product-categories/{id:long}")]
    [ProducesResponseType(typeof(ApiResponse<bool>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<bool>), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ApiResponse<bool>), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> DeleteProductCategory(long id, CancellationToken cancellationToken)
    {
        var response = await productCategoryService.DeleteAsync(id, CurrentUserId, cancellationToken);
        return StatusCode(response.StatusCode, response);
    }

    [Authorize]
    [HttpDelete("product-categories/batch")]
    [ProducesResponseType(typeof(ApiResponse<IList<long>>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<IList<long>>), StatusCodes.Status207MultiStatus)]
    [ProducesResponseType(typeof(ApiResponse<IList<long>>), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> DeleteProductCategoriesBatch([FromBody] IList<long> ids, CancellationToken cancellationToken)
    {
        var response = await productCategoryService.DeleteBatchAsync(ids, CurrentUserId, cancellationToken);
        return StatusCode(response.StatusCode, response);
    }

    [Authorize]
    [HttpGet("inventory/store/{storeId:long}/summary")]
    [ProducesResponseType(typeof(ApiResponse<InventorySummaryResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<InventorySummaryResponse>), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ApiResponse<InventorySummaryResponse>), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ApiResponse<InventorySummaryResponse>), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> GetInventorySummary(long storeId, [FromQuery] InventorySummaryFilterRequest request, CancellationToken cancellationToken)
    {
        var response = await inventoryService.GetSummaryAsync(request, CurrentUserId, storeId, cancellationToken);
        return StatusCode(response.StatusCode, response);
    }

    [Authorize]
    [HttpGet("inventory/store/{storeId:long}/items")]
    [ProducesResponseType(typeof(ApiResponse<PagedResponse<InventoryItemResponse>>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<PagedResponse<InventoryItemResponse>>), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ApiResponse<PagedResponse<InventoryItemResponse>>), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetInventoryItems(long storeId, [FromQuery] PagedQueryRequest request, CancellationToken cancellationToken)
    {
        var response = await inventoryService.GetItemsAsync(request, CurrentUserId, storeId, cancellationToken);
        return StatusCode(response.StatusCode, response);
    }

    [Authorize]
    [HttpPatch("inventory/store/{storeId:long}/product/{productId:long}/stock")]
    [ProducesResponseType(typeof(ApiResponse<InventoryItemResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<InventoryItemResponse>), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ApiResponse<InventoryItemResponse>), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UpdateInventoryStock(long storeId, long productId, [FromBody] UpdateInventoryStockRequest request, CancellationToken cancellationToken)
    {
        var response = await inventoryService.UpdateStockAsync(productId, request, CurrentUserId, storeId, cancellationToken);
        return StatusCode(response.StatusCode, response);
    }

    [Authorize]
    [HttpGet("inventory/stock-adjustment-reasons")]
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

    [Authorize]
    [HttpPost("stocktransfer")]
    [ProducesResponseType(typeof(ApiResponse<StockTransferResponse>), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ApiResponse<StockTransferResponse>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse<StockTransferResponse>), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ApiResponse<StockTransferResponse>), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ApiResponse<StockTransferResponse>), StatusCodes.Status409Conflict)]
    public async Task<IActionResult> CreateStockTransfer([FromBody] CreateStockTransferRequest request, CancellationToken cancellationToken)
    {
        var response = await stockTransferService.CreateAsync(request, CurrentUserId, cancellationToken);
        return StatusCode(response.StatusCode, response);
    }

    [Authorize]
    [HttpGet("stocktransfer/{id:long}")]
    [ProducesResponseType(typeof(ApiResponse<StockTransferResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<StockTransferResponse>), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ApiResponse<StockTransferResponse>), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetStockTransferById(long id, CancellationToken cancellationToken)
    {
        var response = await stockTransferService.GetByIdAsync(id, CurrentUserId, cancellationToken);
        return StatusCode(response.StatusCode, response);
    }

    [Authorize]
    [HttpGet("stocktransfer")]
    [ProducesResponseType(typeof(ApiResponse<PagedResponse<StockTransferResponse>>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetStockTransfers([FromQuery] PagedQueryRequest request, CancellationToken cancellationToken)
    {
        var response = await stockTransferService.GetAllAsync(request, CurrentUserId, cancellationToken);
        return StatusCode(response.StatusCode, response);
    }

    [Authorize]
    [HttpPost("pos/store/{storeId:long}/checkout")]
    [ProducesResponseType(typeof(ApiResponse<SaleResponse>), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ApiResponse<SaleResponse>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse<SaleResponse>), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ApiResponse<SaleResponse>), StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Checkout(long storeId, [FromBody] CreateSaleRequest request, CancellationToken cancellationToken)
    {
        var response = await posService.CreateSaleAsync(request, CurrentUserId, storeId, cancellationToken);
        return StatusCode(response.StatusCode, response);
    }

    [Authorize]
    [HttpGet("pos/sale/{id:long}")]
    [ProducesResponseType(typeof(ApiResponse<SaleResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<SaleResponse>), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ApiResponse<SaleResponse>), StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> GetSaleById(long id, CancellationToken cancellationToken)
    {
        var response = await posService.GetSaleByIdAsync(id, CurrentUserId, cancellationToken);
        return StatusCode(response.StatusCode, response);
    }

    [Authorize]
    [HttpGet("pos/store/{storeId:long}/sales")]
    [ProducesResponseType(typeof(ApiResponse<PagedResponse<SaleResponse>>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<PagedResponse<SaleResponse>>), StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> GetSalesByStore(long storeId, [FromQuery] PagedQueryRequest request, CancellationToken cancellationToken)
    {
        var response = await posService.GetSalesByStoreAsync(request, CurrentUserId, storeId, cancellationToken);
        return StatusCode(response.StatusCode, response);
    }

    [Authorize]
    [HttpGet("pos/sales")]
    [ProducesResponseType(typeof(ApiResponse<PagedResponse<SaleResponse>>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAllSales([FromQuery] PagedQueryRequest request, CancellationToken cancellationToken)
    {
        var response = await posService.GetAllSalesAsync(request, CurrentUserId, cancellationToken);
        return StatusCode(response.StatusCode, response);
    }

    [Authorize]
    [HttpPost("store/{storeId:long}/invoice")]
    [ProducesResponseType(typeof(ApiResponse<InvoiceResponse>), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ApiResponse<InvoiceResponse>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse<InvoiceResponse>), StatusCodes.Status404NotFound)]
    [Consumes("multipart/form-data", "application/json")]
    public async Task<IActionResult> CreateInvoice(long storeId, [FromForm] CreateInvoiceRequest request, CancellationToken cancellationToken)
    {
        var response = await invoiceService.CreateAsync(storeId, CurrentUserId, request, cancellationToken);
        return StatusCode(response.StatusCode, response);
    }

    [Authorize]
    [HttpGet("store/{storeId:long}/invoice/{invoiceId:long}")]
    [ProducesResponseType(typeof(ApiResponse<InvoiceResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<InvoiceResponse>), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetInvoiceById(long storeId, long invoiceId, CancellationToken cancellationToken)
    {
        var response = await invoiceService.GetByIdAsync(storeId, invoiceId, cancellationToken);
        return StatusCode(response.StatusCode, response);
    }

    [Authorize]
    [HttpGet("store/{storeId:long}/invoice")]
    [ProducesResponseType(typeof(ApiResponse<PagedResponse<InvoiceResponse>>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetInvoices(long storeId, [FromQuery] PagedQueryRequest request, CancellationToken cancellationToken)
    {
        var response = await invoiceService.GetAllAsync(storeId, request, cancellationToken);
        return StatusCode(response.StatusCode, response);
    }

    [Authorize]
    [HttpPut("store/{storeId:long}/invoice/{invoiceId:long}")]
    [ProducesResponseType(typeof(ApiResponse<InvoiceResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<InvoiceResponse>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse<InvoiceResponse>), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UpdateInvoice(long storeId, long invoiceId, [FromBody] UpdateInvoiceRequest request, CancellationToken cancellationToken)
    {
        var response = await invoiceService.UpdateAsync(storeId, invoiceId, CurrentUserId, request, cancellationToken);
        return StatusCode(response.StatusCode, response);
    }

    [Authorize]
    [HttpPatch("store/{storeId:long}/invoice/{invoiceId:long}/send")]
    [ProducesResponseType(typeof(ApiResponse<InvoiceResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<InvoiceResponse>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse<InvoiceResponse>), StatusCodes.Status404NotFound)]
    [Consumes("multipart/form-data", "application/json")]
    public async Task<IActionResult> MarkInvoiceAsSent(long storeId, long invoiceId, [FromForm] MarkInvoiceAsSentRequest request, CancellationToken cancellationToken)
    {
        var response = await invoiceService.MarkAsSentAsync(storeId, invoiceId, request, cancellationToken);
        return StatusCode(response.StatusCode, response);
    }

    [Authorize]
    [HttpPatch("store/{storeId:long}/invoice/{invoiceId:long}/payment")]
    [ProducesResponseType(typeof(ApiResponse<InvoiceResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<InvoiceResponse>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse<InvoiceResponse>), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> RecordInvoicePayment(long storeId, long invoiceId, [FromBody] RecordInvoicePaymentRequest request, CancellationToken cancellationToken)
    {
        var response = await invoiceService.RecordPaymentAsync(storeId, invoiceId, request, cancellationToken);
        return StatusCode(response.StatusCode, response);
    }

    [Authorize]
    [HttpPatch("store/{storeId:long}/invoice/{invoiceId:long}/cancel")]
    [ProducesResponseType(typeof(ApiResponse<InvoiceResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<InvoiceResponse>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse<InvoiceResponse>), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> CancelInvoice(long storeId, long invoiceId, CancellationToken cancellationToken)
    {
        var response = await invoiceService.CancelAsync(storeId, invoiceId, cancellationToken);
        return StatusCode(response.StatusCode, response);
    }

    [Authorize]
    [HttpDelete("store/{storeId:long}/invoice/{invoiceId:long}")]
    [ProducesResponseType(typeof(ApiResponse<bool>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<bool>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse<bool>), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeleteInvoice(long storeId, long invoiceId, CancellationToken cancellationToken)
    {
        var response = await invoiceService.DeleteAsync(storeId, invoiceId, cancellationToken);
        return StatusCode(response.StatusCode, response);
    }

    [Authorize]
    [HttpPost("customer/store/{storeId:long}")]
    [ProducesResponseType(typeof(ApiResponse<CustomerResponse>), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ApiResponse<CustomerResponse>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse<CustomerResponse>), StatusCodes.Status409Conflict)]
    [ProducesResponseType(typeof(ApiResponse<CustomerResponse>), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> CreateCustomer(long storeId, [FromBody] CreateCustomerRequest request, CancellationToken cancellationToken)
    {
        var response = await customerService.CreateAsync(request, CurrentUserId, storeId, cancellationToken);
        return StatusCode(response.StatusCode, response);
    }

    [Authorize]
    [HttpGet("customer/{id:long}")]
    [ProducesResponseType(typeof(ApiResponse<CustomerResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<CustomerResponse>), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ApiResponse<CustomerResponse>), StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> GetCustomerById(long id, CancellationToken cancellationToken)
    {
        var response = await customerService.GetByIdAsync(id, CurrentUserId, cancellationToken);
        return StatusCode(response.StatusCode, response);
    }

    [Authorize]
    [HttpGet("customer")]
    [ProducesResponseType(typeof(ApiResponse<PagedResponse<CustomerResponse>>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetCustomers([FromQuery] PagedQueryRequest request, CancellationToken cancellationToken)
    {
        var response = await customerService.GetAllAsync(request, CurrentUserId, cancellationToken);
        return StatusCode(response.StatusCode, response);
    }

    [Authorize]
    [HttpPut("customer/{id:long}")]
    [ProducesResponseType(typeof(ApiResponse<CustomerResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<CustomerResponse>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse<CustomerResponse>), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ApiResponse<CustomerResponse>), StatusCodes.Status409Conflict)]
    [ProducesResponseType(typeof(ApiResponse<CustomerResponse>), StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> UpdateCustomer(long id, [FromBody] CreateCustomerRequest request, CancellationToken cancellationToken)
    {
        var response = await customerService.UpdateAsync(id, request, CurrentUserId, cancellationToken);
        return StatusCode(response.StatusCode, response);
    }

    [Authorize]
    [HttpDelete("customer/{id:long}")]
    [ProducesResponseType(typeof(ApiResponse<bool>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<bool>), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ApiResponse<bool>), StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> DeleteCustomer(long id, CancellationToken cancellationToken)
    {
        var response = await customerService.DeleteAsync(id, CurrentUserId, cancellationToken);
        return StatusCode(response.StatusCode, response);
    }

    [Authorize]
    [HttpPost("supplier")]
    [ProducesResponseType(typeof(ApiResponse<SupplierResponse>), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ApiResponse<SupplierResponse>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse<SupplierResponse>), StatusCodes.Status409Conflict)]
    public async Task<IActionResult> CreateSupplier([FromBody] CreateSupplierRequest request, CancellationToken cancellationToken)
    {
        var response = await supplierService.CreateAsync(request, CurrentUserId, cancellationToken);
        return StatusCode(response.StatusCode, response);
    }

    [Authorize]
    [HttpGet("supplier/{id:long}")]
    [ProducesResponseType(typeof(ApiResponse<SupplierResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<SupplierResponse>), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetSupplierById(long id, CancellationToken cancellationToken)
    {
        var response = await supplierService.GetByIdAsync(id, CurrentUserId, cancellationToken);
        return StatusCode(response.StatusCode, response);
    }

    [Authorize]
    [HttpGet("supplier")]
    [ProducesResponseType(typeof(ApiResponse<PagedResponse<SupplierResponse>>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetSuppliers([FromQuery] PagedQueryRequest request, CancellationToken cancellationToken)
    {
        var response = await supplierService.GetAllAsync(request, CurrentUserId, cancellationToken);
        return StatusCode(response.StatusCode, response);
    }

    [Authorize]
    [HttpPut("supplier/{id:long}")]
    [ProducesResponseType(typeof(ApiResponse<SupplierResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<SupplierResponse>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse<SupplierResponse>), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ApiResponse<SupplierResponse>), StatusCodes.Status409Conflict)]
    public async Task<IActionResult> UpdateSupplier(long id, [FromBody] UpdateSupplierRequest request, CancellationToken cancellationToken)
    {
        var response = await supplierService.UpdateAsync(id, request, CurrentUserId, cancellationToken);
        return StatusCode(response.StatusCode, response);
    }

    [Authorize]
    [HttpDelete("supplier/{id:long}")]
    [ProducesResponseType(typeof(ApiResponse<bool>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<bool>), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeleteSupplier(long id, CancellationToken cancellationToken)
    {
        var response = await supplierService.DeleteAsync(id, CurrentUserId, cancellationToken);
        return StatusCode(response.StatusCode, response);
    }

    [Authorize]
    [HttpGet("bank-accounts")]
    [ProducesResponseType(typeof(ApiResponse<IList<BankAccountResponse>>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetBankAccounts(CancellationToken cancellationToken)
    {
        var response = await bankAccountService.GetAllAsync(CurrentUserId, cancellationToken);
        return StatusCode(response.StatusCode, response);
    }

    [Authorize]
    [HttpPost("bank-accounts")]
    [ProducesResponseType(typeof(ApiResponse<BankAccountResponse>), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ApiResponse<BankAccountResponse>), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> CreateBankAccount([FromBody] CreateBankAccountRequest request, CancellationToken cancellationToken)
    {
        var response = await bankAccountService.CreateAsync(request, CurrentUserId, cancellationToken);
        return StatusCode(response.StatusCode, response);
    }

    [Authorize]
    [HttpPut("bank-accounts/{id:long}")]
    [ProducesResponseType(typeof(ApiResponse<BankAccountResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<BankAccountResponse>), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ApiResponse<BankAccountResponse>), StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> UpdateBankAccount(long id, [FromBody] CreateBankAccountRequest request, CancellationToken cancellationToken)
    {
        var response = await bankAccountService.UpdateAsync(id, request, CurrentUserId, cancellationToken);
        return StatusCode(response.StatusCode, response);
    }

    [Authorize]
    [HttpPatch("bank-accounts/{id:long}/set-default")]
    [ProducesResponseType(typeof(ApiResponse<BankAccountResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<BankAccountResponse>), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ApiResponse<BankAccountResponse>), StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> SetDefaultBankAccount(long id, CancellationToken cancellationToken)
    {
        var response = await bankAccountService.SetDefaultAsync(id, CurrentUserId, cancellationToken);
        return StatusCode(response.StatusCode, response);
    }

    [Authorize]
    [HttpDelete("bank-accounts/{id:long}")]
    [ProducesResponseType(typeof(ApiResponse<bool>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<bool>), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ApiResponse<bool>), StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> DeleteBankAccount(long id, CancellationToken cancellationToken)
    {
        var response = await bankAccountService.DeleteAsync(id, CurrentUserId, cancellationToken);
        return StatusCode(response.StatusCode, response);
    }

    [Authorize]
    [HttpGet("metrics")]
    [ProducesResponseType(typeof(ApiResponse<MetricsResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<MetricsResponse>), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetMetrics([FromQuery] MetricsRequest request, CancellationToken cancellationToken)
    {
        var response = await metricsService.GetAsync(request, CurrentUserId, cancellationToken);
        return StatusCode(response.StatusCode, response);
    }

    [Authorize]
    [HttpGet("metrics/sales-summary")]
    [ProducesResponseType(typeof(ApiResponse<SalesSummaryResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<SalesSummaryResponse>), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetSalesSummary([FromQuery] SalesSummaryRequest request, CancellationToken cancellationToken)
    {
        var response = await metricsService.GetSalesSummaryAsync(request, CurrentUserId, cancellationToken);
        return StatusCode(response.StatusCode, response);
    }

    [Authorize]
    [HttpGet("metrics/sales-graph")]
    [ProducesResponseType(typeof(ApiResponse<SalesGraphResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<SalesGraphResponse>), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetSalesGraph([FromQuery] SalesSummaryRequest request, CancellationToken cancellationToken)
    {
        var response = await metricsService.GetSalesGraphAsync(request, CurrentUserId, cancellationToken);
        return StatusCode(response.StatusCode, response);
    }

    [Authorize]
    [HttpGet("metrics/recent-transactions")]
    [ProducesResponseType(typeof(ApiResponse<PagedResponse<SaleResponse>>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<PagedResponse<SaleResponse>>), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetRecentTransactions([FromQuery] SalesSummaryRequest request, [FromQuery] PagedQueryRequest paging, CancellationToken cancellationToken)
    {
        var response = await metricsService.GetRecentTransactionsAsync(request, paging, CurrentUserId, cancellationToken);
        return StatusCode(response.StatusCode, response);
    }

    [Authorize]
    [HttpGet("metrics/products")]
    [ProducesResponseType(typeof(ApiResponse<ProductMetricsResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<ProductMetricsResponse>), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetProductMetrics([FromQuery] MetricsRequest request, CancellationToken cancellationToken)
    {
        var response = await metricsService.GetProductMetricsAsync(request, CurrentUserId, cancellationToken);
        return StatusCode(response.StatusCode, response);
    }

    [Authorize]
    [HttpGet("metrics/inventory")]
    [ProducesResponseType(typeof(ApiResponse<InventoryMetricsResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<InventoryMetricsResponse>), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetInventoryMetrics([FromQuery] MetricsRequest request, CancellationToken cancellationToken)
    {
        var response = await metricsService.GetInventoryMetricsAsync(request, CurrentUserId, cancellationToken);
        return StatusCode(response.StatusCode, response);
    }

    [Authorize]
    [HttpGet("metrics/customers")]
    [ProducesResponseType(typeof(ApiResponse<CustomerMetricsResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<CustomerMetricsResponse>), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetCustomerMetrics([FromQuery] MetricsRequest request, CancellationToken cancellationToken)
    {
        var response = await metricsService.GetCustomerMetricsAsync(request, CurrentUserId, cancellationToken);
        return StatusCode(response.StatusCode, response);
    }

    [Authorize]
    [HttpGet("metrics/recent-transactions/{id:long}")]
    [ProducesResponseType(typeof(ApiResponse<SaleResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<SaleResponse>), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetTransactionById(long id, CancellationToken cancellationToken)
    {
        var response = await metricsService.GetTransactionByIdAsync(id, CurrentUserId, cancellationToken);
        return StatusCode(response.StatusCode, response);
    }

    [Authorize]
    [HttpGet("metrics/sales-by-location")]
    [ProducesResponseType(typeof(ApiResponse<SalesByLocationResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<SalesByLocationResponse>), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetSalesByLocation([FromQuery] MetricsRequest request, CancellationToken cancellationToken)
    {
        var response = await metricsService.GetSalesByLocationAsync(request, CurrentUserId, cancellationToken);
        return StatusCode(response.StatusCode, response);
    }

    [Authorize]
    [HttpGet("metrics/sales-by-category")]
    [ProducesResponseType(typeof(ApiResponse<SalesByCategoryResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<SalesByCategoryResponse>), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetSalesByCategory([FromQuery] MetricsRequest request, CancellationToken cancellationToken)
    {
        var response = await metricsService.GetSalesByCategoryAsync(request, CurrentUserId, cancellationToken);
        return StatusCode(response.StatusCode, response);
    }

    [Authorize]
    [HttpGet("metrics/payment-methods")]
    [ProducesResponseType(typeof(ApiResponse<PaymentMethodBreakdownResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<PaymentMethodBreakdownResponse>), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetPaymentMethodBreakdown([FromQuery] MetricsRequest request, CancellationToken cancellationToken)
    {
        var response = await metricsService.GetPaymentMethodBreakdownAsync(request, CurrentUserId, cancellationToken);
        return StatusCode(response.StatusCode, response);
    }

    [Authorize]
    [HttpGet("metrics/top-customers")]
    [ProducesResponseType(typeof(ApiResponse<TopCustomersResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<TopCustomersResponse>), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetTopCustomers([FromQuery] MetricsRequest request, CancellationToken cancellationToken)
    {
        var response = await metricsService.GetTopCustomersAsync(request, CurrentUserId, cancellationToken);
        return StatusCode(response.StatusCode, response);
    }

    [Authorize]
    [HttpGet("metrics/customer-growth")]
    [ProducesResponseType(typeof(ApiResponse<CustomerGrowthResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<CustomerGrowthResponse>), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetCustomerGrowth([FromQuery] MetricsRequest request, CancellationToken cancellationToken)
    {
        var response = await metricsService.GetCustomerGrowthAsync(request, CurrentUserId, cancellationToken);
        return StatusCode(response.StatusCode, response);
    }

    [Authorize]
    [HttpGet("metrics/staff")]
    [ProducesResponseType(typeof(ApiResponse<StaffAnalyticsResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<StaffAnalyticsResponse>), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetStaffAnalytics([FromQuery] MetricsRequest request, CancellationToken cancellationToken)
    {
        var response = await metricsService.GetStaffAnalyticsAsync(request, CurrentUserId, cancellationToken);
        return StatusCode(response.StatusCode, response);
    }

    [Authorize]
    [HttpGet("metrics/product-performance")]
    [ProducesResponseType(typeof(ApiResponse<ProductPerformanceResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<ProductPerformanceResponse>), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetProductPerformance([FromQuery] MetricsRequest request, CancellationToken cancellationToken)
    {
        var response = await metricsService.GetProductPerformanceAsync(request, CurrentUserId, cancellationToken);
        return StatusCode(response.StatusCode, response);
    }

    [Authorize]
    [HttpGet("metrics/financial")]
    [ProducesResponseType(typeof(ApiResponse<FinancialMetricsResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<FinancialMetricsResponse>), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetFinancialMetrics([FromQuery] MetricsRequest request, CancellationToken cancellationToken)
    {
        var response = await metricsService.GetFinancialMetricsAsync(request, CurrentUserId, cancellationToken);
        return StatusCode(response.StatusCode, response);
    }

    [Authorize]
    [HttpGet("auditlog/business/{businessId:long}")]
    [ProducesResponseType(typeof(ApiResponse<PagedResponse<AuditLogResponse>>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAuditLogsByBusiness(long businessId, [FromQuery] PagedQueryRequest request, CancellationToken cancellationToken)
    {
        var response = await auditLogService.GetAsync(businessId, null, request, cancellationToken);
        return StatusCode(response.StatusCode, response);
    }

    [Authorize]
    [HttpGet("auditlog/user/{userId:long}")]
    [ProducesResponseType(typeof(ApiResponse<PagedResponse<AuditLogResponse>>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAuditLogsByUser(long userId, [FromQuery] PagedQueryRequest request, CancellationToken cancellationToken)
    {
        var response = await auditLogService.GetAsync(null, userId, request, cancellationToken);
        return StatusCode(response.StatusCode, response);
    }

    [Authorize]
    [HttpGet("notification/store/{storeId:long}")]
    [ProducesResponseType(typeof(ApiResponse<PagedResponse<NotificationResponse>>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetNotificationFeedByStore(long storeId, [FromQuery] NotificationFeedRequest request, CancellationToken cancellationToken)
    {
        var response = await notificationService.GetFeedAsync(CurrentUserId, storeId, request, cancellationToken);
        return StatusCode(response.StatusCode, response);
    }

    [Authorize]
    [HttpGet("notification")]
    [ProducesResponseType(typeof(ApiResponse<PagedResponse<NotificationResponse>>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetNotificationFeed([FromQuery] NotificationFeedRequest request, CancellationToken cancellationToken)
    {
        var response = await notificationService.GetFeedAsync(CurrentUserId, request.StoreId, request, cancellationToken);
        return StatusCode(response.StatusCode, response);
    }

    [Authorize]
    [HttpGet("notification/store/{storeId:long}/unread-count")]
    [ProducesResponseType(typeof(ApiResponse<UnreadCountResponse>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetNotificationUnreadCountByStore(long storeId, CancellationToken cancellationToken)
    {
        var response = await notificationService.GetUnreadCountAsync(CurrentUserId, storeId, cancellationToken);
        return StatusCode(response.StatusCode, response);
    }

    [Authorize]
    [HttpGet("notification/unread-count")]
    [ProducesResponseType(typeof(ApiResponse<UnreadCountResponse>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetNotificationUnreadCount([FromQuery] long? storeId, CancellationToken cancellationToken)
    {
        var response = await notificationService.GetUnreadCountAsync(CurrentUserId, storeId, cancellationToken);
        return StatusCode(response.StatusCode, response);
    }

    [Authorize]
    [HttpPatch("notification/store/{storeId:long}/{id:long}/read")]
    [ProducesResponseType(typeof(ApiResponse<bool>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<bool>), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> MarkNotificationRead(long storeId, long id, CancellationToken cancellationToken)
    {
        var response = await notificationService.MarkReadAsync(CurrentUserId, storeId, id, cancellationToken);
        return StatusCode(response.StatusCode, response);
    }

    [Authorize]
    [HttpPatch("notification/{id:long}/read")]
    [ProducesResponseType(typeof(ApiResponse<bool>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<bool>), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> MarkNotificationReadWithoutStore(long id, [FromQuery] long? storeId, CancellationToken cancellationToken)
    {
        var response = await notificationService.MarkReadAsync(CurrentUserId, storeId, id, cancellationToken);
        return StatusCode(response.StatusCode, response);
    }

    [Authorize]
    [HttpPatch("notification/store/{storeId:long}/read-all")]
    [ProducesResponseType(typeof(ApiResponse<bool>), StatusCodes.Status200OK)]
    public async Task<IActionResult> MarkAllNotificationsReadByStore(long storeId, CancellationToken cancellationToken)
    {
        var response = await notificationService.MarkAllReadAsync(CurrentUserId, storeId, cancellationToken);
        return StatusCode(response.StatusCode, response);
    }

    [Authorize]
    [HttpPatch("notification/read-all")]
    [ProducesResponseType(typeof(ApiResponse<bool>), StatusCodes.Status200OK)]
    public async Task<IActionResult> MarkAllNotificationsRead([FromQuery] long? storeId, CancellationToken cancellationToken)
    {
        var response = await notificationService.MarkAllReadAsync(CurrentUserId, storeId, cancellationToken);
        return StatusCode(response.StatusCode, response);
    }

    [Authorize]
    [HttpDelete("notification/store/{storeId:long}/{id:long}")]
    [ProducesResponseType(typeof(ApiResponse<bool>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<bool>), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeleteNotification(long storeId, long id, CancellationToken cancellationToken)
    {
        var response = await notificationService.DeleteAsync(CurrentUserId, storeId, id, cancellationToken);
        return StatusCode(response.StatusCode, response);
    }

    [Authorize]
    [HttpDelete("notification/{id:long}")]
    [ProducesResponseType(typeof(ApiResponse<bool>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<bool>), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeleteNotificationWithoutStore(long id, [FromQuery] long? storeId, CancellationToken cancellationToken)
    {
        var response = await notificationService.DeleteAsync(CurrentUserId, storeId, id, cancellationToken);
        return StatusCode(response.StatusCode, response);
    }

    [AllowAnonymous]
    [HttpPost("support/contact")]
    public async Task<IActionResult> ContactSupport([FromBody] ContactSupportRequest request, CancellationToken cancellationToken)
    {
        var response = await supportService.ContactAsync(request, cancellationToken);
        return StatusCode(response.StatusCode, response);
    }

    [Authorize]
    [HttpGet("support")]
    public async Task<IActionResult> GetSupportRequests([FromQuery] PagedQueryRequest request, CancellationToken cancellationToken)
    {
        var response = await supportService.GetAllAsync(request, cancellationToken);
        return StatusCode(response.StatusCode, response);
    }

    [AllowAnonymous]
    [HttpPost("waitlist")]
    [ProducesResponseType(typeof(ApiResponse<WaitlistResponse>), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ApiResponse<WaitlistResponse>), StatusCodes.Status409Conflict)]
    public async Task<IActionResult> JoinWaitlist([FromBody] JoinWaitlistRequest request, CancellationToken cancellationToken)
    {
        var response = await waitlistService.JoinAsync(request, cancellationToken);
        return StatusCode(response.StatusCode, response);
    }

    [Authorize]
    [HttpGet("waitlist")]
    [ProducesResponseType(typeof(ApiResponse<PagedResponse<WaitlistResponse>>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetWaitlist([FromQuery] PagedQueryRequest request, CancellationToken cancellationToken)
    {
        var response = await waitlistService.GetAllAsync(request, cancellationToken);
        return StatusCode(response.StatusCode, response);
    }

    [Authorize]
    [HttpPatch("waitlist/{id:long}/contacted")]
    [ProducesResponseType(typeof(ApiResponse<bool>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<bool>), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> MarkWaitlistContacted(long id, CancellationToken cancellationToken)
    {
        var response = await waitlistService.MarkContactedAsync(id, cancellationToken);
        return StatusCode(response.StatusCode, response);
    }

    [Authorize]
    [HttpDelete("waitlist/{id:long}")]
    [ProducesResponseType(typeof(ApiResponse<bool>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<bool>), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeleteWaitlistEntry(long id, CancellationToken cancellationToken)
    {
        var response = await waitlistService.DeleteAsync(id, cancellationToken);
        return StatusCode(response.StatusCode, response);
    }

    [Authorize]
    [HttpGet("onboarding/progress")]
    [ProducesResponseType(typeof(ApiResponse<OnboardingProgressResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<OnboardingProgressResponse>), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetOnboardingProgress(CancellationToken cancellationToken)
    {
        var response = await onboardingProgressService.GetProgressAsync(CurrentUserId, cancellationToken);
        return StatusCode(response.StatusCode, response);
    }
}
