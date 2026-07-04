namespace AutVent.CorePlatform.Domain.Entities;

public class SaleItem : BaseEntity
{
    public long SaleId { get; set; }
    public virtual Sale Sale { get; set; } = null!;
    public long ProductId { get; set; }
    public virtual Product Product { get; set; } = null!;
    public long Quantity { get; set; }
    public decimal UnitPrice { get; set; }
    public decimal LineTotal { get; set; } // Quantity * UnitPrice
}
