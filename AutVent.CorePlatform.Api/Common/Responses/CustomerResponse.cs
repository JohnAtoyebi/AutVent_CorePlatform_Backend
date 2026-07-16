namespace AutVent.CorePlatform.Api.Common.Responses;

public sealed class CustomerResponse
{
    public long CustomerId { get; init; }
    public string FullName { get; init; } = string.Empty;
    public string PhoneNumber { get; init; } = string.Empty;
    public string? Email { get; init; }
    public string? Address { get; init; }
    public long StoreId { get; init; }
}
