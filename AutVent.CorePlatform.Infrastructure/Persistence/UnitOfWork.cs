using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;

namespace AutVent.CorePlatform.Infrastructure.Persistence;

public sealed class UnitOfWork(CorePlatformDbContext dbContext) : IUnitOfWork
{
    public IQueryable<TEntity> Query<TEntity>() where TEntity : class
        => dbContext.Set<TEntity>().AsQueryable();

    public Task<TEntity?> GetByIdAsync<TEntity>(object[] keyValues, CancellationToken cancellationToken = default)
        where TEntity : class
        => dbContext.Set<TEntity>().FindAsync(keyValues, cancellationToken).AsTask();

    public async Task CreateAsync<TEntity>(TEntity entity, CancellationToken cancellationToken = default)
        where TEntity : class
    {
        await dbContext.Set<TEntity>().AddAsync(entity, cancellationToken);
    }

    public async Task CreateRangeAsync<TEntity>(IEnumerable<TEntity> entities, CancellationToken cancellationToken = default)
        where TEntity : class
    {
        await dbContext.Set<TEntity>().AddRangeAsync(entities, cancellationToken);
    }

    public void Update<TEntity>(TEntity entity) where TEntity : class
    {
        dbContext.Set<TEntity>().Update(entity);
    }

    public void Delete<TEntity>(TEntity entity) where TEntity : class
    {
        dbContext.Set<TEntity>().Remove(entity);
    }

    public void DeleteRange<TEntity>(IEnumerable<TEntity> entities) where TEntity : class
    {
        dbContext.Set<TEntity>().RemoveRange(entities);
    }

    public Task<IDbContextTransaction> BeginTransactionAsync(CancellationToken cancellationToken = default)
        => dbContext.Database.BeginTransactionAsync(cancellationToken);

    public Task CommitTransactionAsync(IDbContextTransaction transaction, CancellationToken cancellationToken = default)
        => transaction.CommitAsync(cancellationToken);

    public Task RollbackTransactionAsync(IDbContextTransaction transaction, CancellationToken cancellationToken = default)
        => transaction.RollbackAsync(cancellationToken);

    public Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
        => dbContext.SaveChangesAsync(cancellationToken);
}
