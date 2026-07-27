namespace AutVent.Domain.Entities;

public sealed class Store
{
    public Guid Id { get; set; }

    /// <summary>Server-side integer id from the API (storeId).</summary>
    public long RemoteId { get; set; }

    public string Name { get; set; } = string.Empty;

    public string Address { get; set; } = string.Empty;

    public string City { get; set; } = string.Empty;

    public string PhoneNumber { get; set; } = string.Empty;

    public bool IsActive { get; set; }

    public DateTime UpdatedAtUtc { get; set; }
}
