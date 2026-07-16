using AutVent.CorePlatform.Domain.Enums;

namespace AutVent.CorePlatform.Domain.Entities;

public class StockTransfer : BaseEntity
{
    public string TransferNumber { get; set; } = string.Empty;
    public long SourceStoreId { get; set; }
    public virtual Store SourceStore { get; set; } = null!;
    public long DestinationStoreId { get; set; }
    public virtual Store DestinationStore { get; set; } = null!;
    public DateTime TransferDate { get; set; }
    public StockTransferStatus Status { get; set; }
    public string? Notes { get; set; }
    public virtual ICollection<StockTransferItem> Items { get; set; } = new List<StockTransferItem>();
}
