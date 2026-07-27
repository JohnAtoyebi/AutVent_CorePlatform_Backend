using AutVent.Domain.Enums;

namespace AutVent.Domain.Entities;

public sealed class Sale
{
    public Guid Id { get; set; }

    /// <summary>Server-side saleId from SaleResponse.</summary>
    public long RemoteId { get; set; }

    public string SaleNumber { get; set; } = string.Empty;

    public Guid StoreId { get; set; }

    public long RemoteStoreId { get; set; }

    public Guid? CustomerId { get; set; }

    public decimal Subtotal { get; set; }

    public decimal DiscountAmount { get; set; }

    public decimal TaxAmount { get; set; }

    public decimal TotalAmount { get; set; }

    public PaymentMethod PaymentMethod { get; set; }

    public bool IsSynced { get; set; }

    public DateTime CreatedAtUtc { get; set; }

    public ICollection<SaleItem> Items { get; set; } = new List<SaleItem>();
}
