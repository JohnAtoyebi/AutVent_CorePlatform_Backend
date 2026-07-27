namespace AutVent.CorePlatform.Api.Common.Responses;

public sealed class AuditLogResponse
{
    public long Id { get; init; }
    public long UserId { get; init; }
    public string UserName { get; init; } = string.Empty;
    public long? BusinessId { get; init; }
    public string Action { get; init; } = string.Empty;
    public string EntityType { get; init; } = string.Empty;
    public long? EntityId { get; init; }
    public string Description { get; init; } = string.Empty;
    public string? OldValues { get; init; }
    public string? NewValues { get; init; }
    public string? IpAddress { get; init; }
    public DateTime Timestamp { get; init; }
}
