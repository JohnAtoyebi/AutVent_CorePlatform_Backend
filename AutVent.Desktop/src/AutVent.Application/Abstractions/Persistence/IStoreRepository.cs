using AutVent.Domain.Entities;

namespace AutVent.Application.Abstractions.Persistence;

public interface IStoreRepository
{
    Task UpsertAsync(IEnumerable<Store> stores, CancellationToken cancellationToken);

    Task<IReadOnlyList<Store>> ListAsync(CancellationToken cancellationToken);
}
