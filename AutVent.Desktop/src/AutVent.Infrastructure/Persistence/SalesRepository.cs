using AutVent.Application.Abstractions.Persistence;
using AutVent.Domain.Entities;
using AutVent.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace AutVent.Infrastructure.Persistence;

public sealed class SalesRepository : ISalesRepository
{
    private readonly AutVentDbContext _dbContext;

    public SalesRepository(AutVentDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public Task AddAsync(Sale sale, CancellationToken cancellationToken)
    {
        _dbContext.Sales.Add(sale);
        return Task.CompletedTask;
    }

    public async Task<IReadOnlyList<Sale>> GetPendingSyncSalesAsync(CancellationToken cancellationToken)
        => await _dbContext.Sales.Include(x => x.Items).Where(x => !x.IsSynced).OrderBy(x => x.Id).ToListAsync(cancellationToken);

    public async Task MarkAsSyncedAsync(Guid saleId, CancellationToken cancellationToken)
    {
        var sale = await _dbContext.Sales.FirstOrDefaultAsync(x => x.Id == saleId, cancellationToken);
        if (sale is not null)
        {
            sale.IsSynced = true;
        }
    }
}
