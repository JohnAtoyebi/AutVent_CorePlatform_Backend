using System.ComponentModel.DataAnnotations;

namespace AutVent.CorePlatform.Api.Common.Requests;

public sealed class CreateStockTransferRequest
{
    [Required]
    public long SourceStoreId { get; init; }

    [Required]
    public long DestinationStoreId { get; init; }

    public DateTime? TransferDate { get; init; }

    [MaxLength(500)]
    public string? Notes { get; init; }

    [Required]
    [MinLength(1)]
    public List<StockTransferItemRequest> Items { get; init; } = [];
}

public sealed class StockTransferItemRequest
{
    [Required]
    public long SourceProductId { get; init; }

    [Required]
    [Range(1, long.MaxValue)]
    public long Quantity { get; init; }
}
