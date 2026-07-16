using AutVent.CorePlatform.Domain.Enums;

namespace AutVent.CorePlatform.Api.Common.Responses;

public sealed class StockTransferResponse
{
    public long TransferId { get; init; }
    public string TransferNumber { get; init; } = string.Empty;
    public long SourceStoreId { get; init; }
    public string SourceStoreName { get; init; } = string.Empty;
    public long DestinationStoreId { get; init; }
    public string DestinationStoreName { get; init; } = string.Empty;
    public DateTime TransferDate { get; init; }
    public StockTransferStatus Status { get; init; }
    public string? Notes { get; init; }
    public List<StockTransferItemResponse> Items { get; init; } = [];
    public DateTime CreatedAt { get; init; }
}

public sealed class StockTransferItemResponse
{
    public long ItemId { get; init; }
    public long SourceProductId { get; init; }
    public string SourceProductName { get; init; } = string.Empty;
    public long DestinationProductId { get; init; }
    public string DestinationProductName { get; init; } = string.Empty;
    public long Quantity { get; init; }
    public string? Notes { get; init; }
}
