using AutVent.CorePlatform.Domain.Enums;

namespace AutVent.CorePlatform.Domain.Entities;

public class Invoice : BaseEntity
{
    public string InvoiceNumber { get; set; } = string.Empty;
    public long StoreId { get; set; }
    public virtual Store Store { get; set; } = null!;
    public long? CustomerId { get; set; }
    public virtual Customer? Customer { get; set; }
    public DateTime IssueDate { get; set; }
    public DateTime DueDate { get; set; }
    public InvoicePaymentTerms PaymentTerms { get; set; }
    public decimal SubTotal { get; set; }
    public SaleDiscountType? DiscountType { get; set; }
    public decimal DiscountValue { get; set; }
    public decimal DiscountAmount { get; set; }
    public decimal VatRate { get; set; } = 7.5m;
    public decimal VatAmount { get; set; }
    public decimal TotalAmount { get; set; }
    public decimal AmountPaid { get; set; }
    public decimal BalanceRemaining { get; set; }
    public SalePaymentMethod PaymentMethod { get; set; }
    public InvoiceStatus Status { get; set; } = InvoiceStatus.Draft;
    public string? Notes { get; set; }
    public virtual ICollection<InvoiceItem> InvoiceItems { get; set; } = new List<InvoiceItem>();
}
