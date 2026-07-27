using AutVent.Application.Abstractions.Persistence;
using AutVent.Domain.Entities;
using AutVent.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace AutVent.Infrastructure.Persistence;

public sealed class ProductRepository : IProductRepository
{
    private readonly AutVentDbContext _dbContext;

    public ProductRepository(AutVentDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<IReadOnlyList<Product>> SearchAsync(string? searchTerm, CancellationToken cancellationToken)
    {
        var query = _dbContext.Products.AsNoTracking();

        if (!string.IsNullOrWhiteSpace(searchTerm))
        {
            var value = searchTerm.Trim();
            query = query.Where(x => x.Name.Contains(value) || x.Sku.Contains(value));
        }

        return await query.OrderBy(x => x.Id).Take(200).ToListAsync(cancellationToken);
    }

    public async Task UpsertAsync(IEnumerable<Product> products, CancellationToken cancellationToken)
    {
        foreach (var product in products)
        {
            // Match by RemoteId if available, otherwise fall back to local Guid
            var existing = product.RemoteId != 0
                ? await _dbContext.Products.FirstOrDefaultAsync(x => x.RemoteId == product.RemoteId, cancellationToken)
                : await _dbContext.Products.FirstOrDefaultAsync(x => x.Id == product.Id, cancellationToken);

            if (existing is null)
            {
                _dbContext.Products.Add(product);
                continue;
            }

            existing.RemoteId = product.RemoteId;
            existing.RemoteStoreId = product.RemoteStoreId;
            existing.Name = product.Name;
            existing.Sku = product.Sku;
            existing.Barcode = product.Barcode;
            existing.Description = product.Description;
            existing.CategoryName = product.CategoryName;
            existing.UnitPrice = product.UnitPrice;
            existing.CostPrice = product.CostPrice;
            existing.TaxRate = product.TaxRate;
            existing.QuantityOnHand = product.QuantityOnHand;
            existing.ReorderThreshold = product.ReorderThreshold;
            existing.AvailableOnPos = product.AvailableOnPos;
            existing.IsActive = product.IsActive;
            existing.UpdatedAtUtc = product.UpdatedAtUtc;
        }
    }
}
