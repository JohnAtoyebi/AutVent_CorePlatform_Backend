namespace AutVent.CorePlatform.Api.Common.Responses;

public sealed class BusinessStoreResponse
{
    public long StoreId { get; init; }
    public string Name { get; init; } = string.Empty;
    public string StoreCategory { get; init; } = string.Empty;
    public string EmailAddress { get; init; } = string.Empty;
    public string PhoneNumber { get; init; } = string.Empty;
    public string? Address { get; init; }
    public string? City { get; init; }
    public string? State { get; init; }
    public string? Country { get; init; }
    public bool IsActive { get; init; }
    public DateTime CreatedAt { get; init; }
}