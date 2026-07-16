namespace AutVent.CorePlatform.Domain.Entities;

public class InvoiceItem : BaseEntity
{
    public long InvoiceId { get; set; }
    public virtual Invoice Invoice { get; set; } = null!;
    public long ProductId { get; set; }
    public virtual Product Product { get; set; } = null!;
    public long Quantity { get; set; }
    public decimal UnitPrice { get; set; }
    public decimal Discount { get; set; }
    public decimal LineTotal { get; set; } // (Quantity * UnitPrice) - Discount
}
