using AutVent.Application.Contracts;
using AutVent.Domain.Entities;
using AutVent.Shared.Results;

namespace AutVent.Application.Abstractions.Services;

public interface IInventoryService
{
    Task<IReadOnlyList<InventoryRecord>> GetInventoryAsync(Guid storeId, string? searchTerm, CancellationToken cancellationToken);

    Task<IReadOnlyList<InventoryItemDto>> GetInventoryItemsAsync(Guid storeId, string? searchTerm, CancellationToken cancellationToken);

    Task<Result> AdjustStockAsync(AdjustInventoryCommand command, CancellationToken cancellationToken);
}
