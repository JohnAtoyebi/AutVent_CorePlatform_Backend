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

    [Required]
    [EnumDataType(typeof(StockAdjustmentReason))]
    public StockAdjustmentReason Reason { get; init; }

    [Required]
    public long LocationStoreId { get; init; }

    [MaxLength(500)]
    public string? Notes { get; init; }
}
