using System.ComponentModel.DataAnnotations;

namespace AutVent.CorePlatform.Api.Common.Requests;

public sealed class CreateProductVariantRequest
{
    [MaxLength(200)]
    public string? Variant { get; init; }

    [MaxLength(100)]
    public string? Sku { get; init; }

    [MaxLength(50)]
    [RegularExpression(@"^[\d,]+(\.\d+)?$", ErrorMessage = "Price must contain numbers only (e.g. 1000 or 1,000.50)")]
    public string? Price { get; init; }

    [Range(0, long.MaxValue)]
    public long? Quantity { get; init; }
}

public sealed class CreateProductRequest
{
    [Required]
    [MaxLength(200)]
    public string Name { get; init; } = string.Empty;

    [Required]
    [MaxLength(50)]
    [RegularExpression(@"^[\d,]+(\.\d+)?$", ErrorMessage = "Price must contain numbers only (e.g. 1000 or 1,000.50)")]
    public string Price { get; init; } = string.Empty;

    [Range(1, long.MaxValue)]
    public long Quantity { get; init; }

    [Required]
    public long ProductCategoryId { get; init; }

    [MaxLength(100)]
    public string? Sku { get; init; }

    [MaxLength(1000)]
    public string? Description { get; init; }

    [MaxLength(100)]
    public string? Barcode { get; init; }

    [MaxLength(50)]
    [RegularExpression(@"^[\d,]+(\.\d+)?$", ErrorMessage = "Cost price must contain numbers only (e.g. 1000 or 1,000.50)")]
    public string? CostPrice { get; init; }

    [MaxLength(50)]
    [RegularExpression(@"^[\d,]+(\.\d+)?$", ErrorMessage = "Compare at price must contain numbers only (e.g. 1000 or 1,000.50)")]
    public string? CompareAtPrice { get; init; }

    public List<string>? ProductImages { get; init; }

    public bool? ProductVariantsEnabled { get; init; }

    public List<CreateProductVariantRequest>? ProductVariants { get; init; }

    public bool? ActiveProduct { get; init; } = true;

    public bool? AvailableOnPos { get; init; } = true;

    public bool? AvailableOnAutShop { get; init; } = true;

    [Range(0, long.MaxValue)]
    public long? ReorderThreshold { get; init; }

    [Range(1, long.MaxValue)]
    public long? StoreId { get; init; }

    public bool? ApplyToAllStoreLocations { get; init; }

    public List<string>? Tags { get; init; }

    [Range(0, double.MaxValue)]
    public decimal? Weight { get; init; }

    [Range(1, long.MaxValue)]
    public long? SupplierId { get; init; }
}
