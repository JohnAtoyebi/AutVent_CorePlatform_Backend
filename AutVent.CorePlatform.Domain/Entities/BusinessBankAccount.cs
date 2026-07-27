namespace AutVent.CorePlatform.Domain.Entities;

public class BusinessBankAccount : BaseEntity
{
    public long BusinessId { get; set; }
    public virtual Business Business { get; set; } = null!;

    public string BankName { get; set; } = string.Empty;
    public string AccountNumber { get; set; } = string.Empty;
    public string AccountName { get; set; } = string.Empty;

    /// <summary>Optional sort code / routing number.</summary>
    public string? SortCode { get; set; }

    /// <summary>When true this account is shown first on invoices.</summary>
    public bool IsDefault { get; set; }
}
