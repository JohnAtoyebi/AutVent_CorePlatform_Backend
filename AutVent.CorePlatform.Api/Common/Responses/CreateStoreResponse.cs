namespace AutVent.CorePlatform.Api.Common.Responses;

public sealed class CreateStoreResponse
{
    public long StoreId { get; init; }
    public long BusinessId { get; init; }
    public string Name { get; init; } = string.Empty;
    public string StoreCategory { get; init; } = string.Empty;
    public string EmailAddress { get; init; } = string.Empty;
    public string PhoneNumber { get; init; } = string.Empty;
}
