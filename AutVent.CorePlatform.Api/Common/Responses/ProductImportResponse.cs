namespace AutVent.CorePlatform.Api.Common.Responses;

public sealed class ProductImportResponse
{
    public int ImportedCount { get; init; }
    public IReadOnlyCollection<ProductResponse> ImportedProducts { get; init; } = [];
}
