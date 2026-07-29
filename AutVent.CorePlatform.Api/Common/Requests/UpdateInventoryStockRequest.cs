using System.ComponentModel.DataAnnotations;
using AutVent.CorePlatform.Domain.Enums;

namespace AutVent.CorePlatform.Api.Common.Requests;

public sealed class UpdateInventoryStockRequest
{
    [Required]
    [EnumDataType(typeof(StockAdjustmentType))]
    public StockAdjustmentType Type { get; init; }

    [Required]
    [Range(1, long.MaxValue)]
    public long Quantity { get; init; }

    [Range(0.01, double.MaxValue)]
    public decimal? PurchaseCostPerUnit { get; init; }

    /// <summary>
    /// Determines how the new purchase price affects the product price on StockIn.
    /// WeightedAverage (default): blends old and new price proportionally by quantity.
    /// Replace: the new price replaces the current price outright.
    /// Only applied when PurchaseCostPerUnit is provided and Type is StockIn.
    /// </summary>
    [EnumDataType(typeof(StockPricingStrategy))]
    public StockPricingStrategy PricingStrategy { get; init; } = StockPricingStrategy.WeightedAverage;

    [Required]
    [EnumDataType(typeof(StockAdjustmentReason))]
    public StockAdjustmentReason Reason { get; init; }

    [Required]
    public long LocationStoreId { get; init; }

    [MaxLength(500)]
    public string? Notes { get; init; }
}
