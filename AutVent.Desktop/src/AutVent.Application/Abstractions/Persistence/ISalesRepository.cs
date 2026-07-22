using AutVent.Domain.Entities;

namespace AutVent.Application.Abstractions.Persistence;

public interface ISalesRepository
{
    Task AddAsync(Sale sale, CancellationToken cancellationToken);

    Task<IReadOnlyList<Sale>> GetPendingSyncSalesAsync(CancellationToken cancellationToken);

    Task MarkAsSyncedAsync(Guid saleId, CancellationToken cancellationToken);
}
