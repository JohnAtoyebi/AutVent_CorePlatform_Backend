using AutVent.Application.Abstractions.Persistence;
using AutVent.Domain.Entities;
using AutVent.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace AutVent.Infrastructure.Persistence;

public sealed class InventoryRepository : IInventoryRepository
{
    private readonly AutVentDbContext _dbContext;

    public InventoryRepository(AutVentDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<IReadOnlyList<InventoryRecord>> GetByStoreAsync(Guid storeId, string? searchTerm, CancellationToken cancellationToken)
    {
        var query = from inventory in _dbContext.Inventory.AsNoTracking()
                    join product in _dbContext.Products.AsNoTracking() on inventory.ProductId equals product.Id
                    where inventory.StoreId == storeId
                    select new { inventory, product };

        if (!string.IsNullOrWhiteSpace(searchTerm))
        {
            var value = searchTerm.Trim();
            query = query.Where(x => x.product.Name.Contains(value) || x.product.Sku.Contains(value));
        }

        return await query.OrderBy(x => x.inventory.Id).Select(x => x.inventory).ToListAsync(cancellationToken);
    }

    public Task<InventoryRecord?> GetByProductAsync(Guid storeId, Guid productId, CancellationToken cancellationToken)
        => _dbContext.Inventory.FirstOrDefaultAsync(x => x.StoreId == storeId && x.ProductId == productId, cancellationToken);

    public async Task UpsertAsync(IEnumerable<InventoryRecord> records, CancellationToken cancellationToken)
    {
        foreach (var record in records)
        {
            // Resolve local ProductId from RemoteProductId if needed
            if (record.ProductId == Guid.Empty && record.RemoteProductId != 0)
            {
                var product = await _dbContext.Products
                    .FirstOrDefaultAsync(x => x.RemoteId == record.RemoteProductId, cancellationToken);
                if (product is not null)
                {
                    record.ProductId = product.Id;
                }
            }

            var existing = (record.RemoteStoreId != 0 && record.RemoteProductId != 0)
                ? await _dbContext.Inventory.FirstOrDefaultAsync(
                    x => x.RemoteStoreId == record.RemoteStoreId && x.RemoteProductId == record.RemoteProductId,
                    cancellationToken)
                : await _dbContext.Inventory.FirstOrDefaultAsync(x => x.Id == record.Id, cancellationToken);

            if (existing is null)
            {
                _dbContext.Inventory.Add(record);
                continue;
            }

            existing.ProductId = record.ProductId == Guid.Empty ? existing.ProductId : record.ProductId;
            existing.StoreId = record.StoreId == Guid.Empty ? existing.StoreId : record.StoreId;
            existing.RemoteProductId = record.RemoteProductId;
            existing.RemoteStoreId = record.RemoteStoreId;
            existing.QuantityOnHand = record.QuantityOnHand;
            existing.ReorderLevel = record.ReorderLevel;
            existing.IsLowStock = record.IsLowStock;
            existing.UpdatedAtUtc = record.UpdatedAtUtc;
        }
    }
}
