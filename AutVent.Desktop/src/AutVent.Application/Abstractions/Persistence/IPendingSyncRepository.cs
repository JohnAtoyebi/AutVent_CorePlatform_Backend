using AutVent.Domain.Entities;

namespace AutVent.Application.Abstractions.Persistence;

public interface IPendingSyncRepository
{
    Task<bool> ExistsDeduplicationKeyAsync(string deduplicationKey, CancellationToken cancellationToken);

    Task AddAsync(PendingSyncOperation operation, CancellationToken cancellationToken);

    Task<IReadOnlyList<PendingSyncOperation>> GetPendingAsync(CancellationToken cancellationToken);

    Task MarkCompletedAsync(Guid operationId, CancellationToken cancellationToken);

    Task IncrementRetryAsync(Guid operationId, CancellationToken cancellationToken);
}
