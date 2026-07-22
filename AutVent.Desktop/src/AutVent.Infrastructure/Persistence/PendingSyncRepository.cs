using AutVent.Application.Abstractions.Persistence;
using AutVent.Domain.Entities;
using AutVent.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace AutVent.Infrastructure.Persistence;

public sealed class PendingSyncRepository : IPendingSyncRepository
{
    private readonly AutVentDbContext _dbContext;

    public PendingSyncRepository(AutVentDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public Task<bool> ExistsDeduplicationKeyAsync(string deduplicationKey, CancellationToken cancellationToken)
        => _dbContext.PendingSync.AnyAsync(x => x.DeduplicationKey == deduplicationKey, cancellationToken);

    public Task AddAsync(PendingSyncOperation operation, CancellationToken cancellationToken)
    {
        _dbContext.PendingSync.Add(operation);
        return Task.CompletedTask;
    }

    public async Task<IReadOnlyList<PendingSyncOperation>> GetPendingAsync(CancellationToken cancellationToken)
        => await _dbContext.PendingSync.Where(x => !x.IsCompleted).OrderBy(x => x.Id).Take(200).ToListAsync(cancellationToken);

    public async Task MarkCompletedAsync(Guid operationId, CancellationToken cancellationToken)
    {
        var existing = await _dbContext.PendingSync.FirstOrDefaultAsync(x => x.Id == operationId, cancellationToken);
        if (existing is not null)
        {
            existing.IsCompleted = true;
            existing.LastAttemptUtc = DateTime.UtcNow;
        }
    }

    public async Task IncrementRetryAsync(Guid operationId, CancellationToken cancellationToken)
    {
        var existing = await _dbContext.PendingSync.FirstOrDefaultAsync(x => x.Id == operationId, cancellationToken);
        if (existing is not null)
        {
            existing.RetryCount += 1;
            existing.LastAttemptUtc = DateTime.UtcNow;
        }
    }
}
