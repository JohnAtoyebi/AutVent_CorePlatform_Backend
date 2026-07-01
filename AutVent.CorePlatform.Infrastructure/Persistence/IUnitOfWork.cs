using Microsoft.EntityFrameworkCore.Storage;

namespace AutVent.CorePlatform.Infrastructure.Persistence;

public interface IUnitOfWork
{
    IQueryable<TEntity> Query<TEntity>() where TEntity : class;
    Task<TEntity?> GetByIdAsync<TEntity>(object[] keyValues, CancellationToken cancellationToken = default) where TEntity : class;
    Task CreateAsync<TEntity>(TEntity entity, CancellationToken cancellationToken = default) where TEntity : class;
    Task CreateRangeAsync<TEntity>(IEnumerable<TEntity> entities, CancellationToken cancellationToken = default) where TEntity : class;
    void Update<TEntity>(TEntity entity) where TEntity : class;
    void Delete<TEntity>(TEntity entity) where TEntity : class;
    void DeleteRange<TEntity>(IEnumerable<TEntity> entities) where TEntity : class;
    Task<IDbContextTransaction> BeginTransactionAsync(CancellationToken cancellationToken = default);
    Task CommitTransactionAsync(IDbContextTransaction transaction, CancellationToken cancellationToken = default);
    Task RollbackTransactionAsync(IDbContextTransaction transaction, CancellationToken cancellationToken = default);
    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}
