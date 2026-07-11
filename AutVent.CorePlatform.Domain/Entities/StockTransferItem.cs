namespace AutVent.CorePlatform.Domain.Entities;

public class StockTransferItem : BaseEntity
{
    public long StockTransferId { get; set; }
    public virtual StockTransfer StockTransfer { get; set; } = null!;
    public long SourceProductId { get; set; }
    public virtual Product SourceProduct { get; set; } = null!;
    public long DestinationProductId { get; set; }
    public virtual Product DestinationProduct { get; set; } = null!;
    public long Quantity { get; set; }
    public string? Notes { get; set; }
}
