using AutVent.Application.Abstractions.Persistence;
using AutVent.Domain.Entities;
using AutVent.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace AutVent.Infrastructure.Persistence;

public sealed class StoreRepository : IStoreRepository
{
    private readonly AutVentDbContext _dbContext;

    public StoreRepository(AutVentDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task UpsertAsync(IEnumerable<Store> stores, CancellationToken cancellationToken)
    {
        foreach (var store in stores)
        {
            var existing = store.RemoteId != 0
                ? await _dbContext.Stores.FirstOrDefaultAsync(x => x.RemoteId == store.RemoteId, cancellationToken)
                : await _dbContext.Stores.FirstOrDefaultAsync(x => x.Id == store.Id, cancellationToken);

            if (existing is null)
            {
                _dbContext.Stores.Add(store);
                continue;
            }

            existing.RemoteId = store.RemoteId;
            existing.Name = store.Name;
            existing.Address = store.Address;
            existing.City = store.City;
            existing.PhoneNumber = store.PhoneNumber;
            existing.IsActive = store.IsActive;
            existing.UpdatedAtUtc = store.UpdatedAtUtc;
        }
    }

    public Task<IReadOnlyList<Store>> ListAsync(CancellationToken cancellationToken)
        => _dbContext.Stores.AsNoTracking().OrderBy(x => x.Id).ToListAsync(cancellationToken).ContinueWith(t => (IReadOnlyList<Store>)t.Result, cancellationToken);
}
