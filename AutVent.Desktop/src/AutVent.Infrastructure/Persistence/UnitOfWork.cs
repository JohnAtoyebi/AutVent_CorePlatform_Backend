using AutVent.Application.Abstractions.Persistence;
using AutVent.Infrastructure.Data;

namespace AutVent.Infrastructure.Persistence;

public sealed class UnitOfWork : IUnitOfWork
{
    private readonly AutVentDbContext _dbContext;

    public UnitOfWork(AutVentDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public Task<int> SaveChangesAsync(CancellationToken cancellationToken)
        => _dbContext.SaveChangesAsync(cancellationToken);
}
