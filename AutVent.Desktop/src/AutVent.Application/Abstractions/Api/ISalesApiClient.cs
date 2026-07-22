using AutVent.Application.Contracts;

namespace AutVent.Application.Abstractions.Api;

public interface ISalesApiClient
{
    /// <summary>POST /api/Pos/store/{storeId}/checkout — returns the completed sale.</summary>
    Task<CompletedSaleDto> CheckoutAsync(CompleteSaleCommand command, CancellationToken cancellationToken);
}
