using AutVent.CorePlatform.Domain.Enums;

namespace AutVent.CorePlatform.Domain.Entities;

public class Sale : BaseEntity
{
    public string SaleNumber { get; set; } = string.Empty;
    public long StoreId { get; set; }
    public virtual Store Store { get; set; } = null!;
    public long? CustomerId { get; set; }
    public virtual Customer? Customer { get; set; }
    public decimal SubTotal { get; set; }
    public SaleDiscountType? DiscountType { get; set; }
    public decimal DiscountValue { get; set; }
    public decimal DiscountAmount { get; set; }
    public decimal TaxAmount { get; set; }
    public decimal TotalAmount { get; set; }
    public decimal AmountPaid { get; set; }
    public decimal BalanceRemaining { get; set; }
    public decimal ChangeAmount { get; set; }
    public SalePaymentMethod PaymentMethod { get; set; }
    public string? Notes { get; set; }
    public virtual ICollection<SaleItem> SaleItems { get; set; } = new List<SaleItem>();
}
