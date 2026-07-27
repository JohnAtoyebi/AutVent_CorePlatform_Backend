namespace AutVent.CorePlatform.Api.Common.Responses;

public sealed class BankAccountResponse
{
    public long Id { get; init; }
    public string BankName { get; init; } = string.Empty;
    public string AccountNumber { get; init; } = string.Empty;
    public string AccountName { get; init; } = string.Empty;
    public string? SortCode { get; init; }
    public bool IsDefault { get; init; }
}
