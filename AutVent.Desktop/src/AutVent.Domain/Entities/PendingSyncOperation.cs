using AutVent.Domain.Enums;

namespace AutVent.Domain.Entities;

public sealed class PendingSyncOperation
{
    public Guid Id { get; set; }

    public SyncOperationType OperationType { get; set; }

    public string PayloadJson { get; set; } = string.Empty;

    public int RetryCount { get; set; }

    public DateTime CreatedAtUtc { get; set; }

    public DateTime? LastAttemptUtc { get; set; }

    public bool IsCompleted { get; set; }

    public string DeduplicationKey { get; set; } = string.Empty;
}
