using AutVent.CorePlatform.Api.Common.Requests;
using AutVent.CorePlatform.Api.Common.Responses;
using AutVent.CorePlatform.Domain.Enums;

namespace AutVent.CorePlatform.Api.Services;

public interface IAuditLogService
{
    Task LogAsync(
        long userId,
        AuditAction action,
        string entityType,
        string description,
        long? businessId = null,
        long? entityId = null,
        string? oldValues = null,
        string? newValues = null,
        string? ipAddress = null,
        CancellationToken cancellationToken = default);

    Task<ApiResponse<PagedResponse<AuditLogResponse>>> GetAsync(
        long? businessId,
        long? userId,
        PagedQueryRequest request,
        CancellationToken cancellationToken = default);
}
