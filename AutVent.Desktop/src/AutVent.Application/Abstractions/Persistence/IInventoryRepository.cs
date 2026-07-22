using AutVent.Domain.Entities;

namespace AutVent.Application.Abstractions.Persistence;

public interface IInventoryRepository
{
    Task<IReadOnlyList<InventoryRecord>> GetByStoreAsync(Guid storeId, string? searchTerm, CancellationToken cancellationToken);

    Task<InventoryRecord?> GetByProductAsync(Guid storeId, Guid productId, CancellationToken cancellationToken);

    Task UpsertAsync(IEnumerable<InventoryRecord> records, CancellationToken cancellationToken);
}
