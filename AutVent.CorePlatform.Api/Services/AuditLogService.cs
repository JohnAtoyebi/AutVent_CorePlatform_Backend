using AutVent.CorePlatform.Api.Common.Requests;
using AutVent.CorePlatform.Api.Common.Responses;
using AutVent.CorePlatform.Domain.Entities;
using AutVent.CorePlatform.Domain.Enums;
using AutVent.CorePlatform.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace AutVent.CorePlatform.Api.Services;

public sealed class AuditLogService(IUnitOfWork unitOfWork) : IAuditLogService
{
    private const string SystemActor = "system";

    public async Task LogAsync(
        long userId,
        AuditAction action,
        string entityType,
        string description,
        long? businessId = null,
        long? entityId = null,
        string? oldValues = null,
        string? newValues = null,
        string? ipAddress = null,
        CancellationToken cancellationToken = default)
    {
        var now = DateTime.UtcNow;

        var log = new AuditLog
        {
            UserId = userId,
            BusinessId = businessId,
            Action = action,
            EntityType = entityType,
            EntityId = entityId,
            Description = description,
            OldValues = oldValues,
            NewValues = newValues,
            IpAddress = ipAddress,
            IsActive = true,
            CreatedBy = SystemActor,
            DateCreated = now
        };

        await unitOfWork.CreateAsync(log, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);
    }

    public async Task<ApiResponse<PagedResponse<AuditLogResponse>>> GetAsync(
        long? businessId,
        long? userId,
        PagedQueryRequest request,
        CancellationToken cancellationToken = default)
    {
        var pageNumber = Math.Max(1, request.PageNumber);
        var pageSize = Math.Clamp(request.PageSize, 1, 100);

        var query = unitOfWork.Query<AuditLog>()
            .Include(x => x.User)
            .AsQueryable();

        if (businessId.HasValue)
            query = query.Where(x => x.BusinessId == businessId.Value);

        if (userId.HasValue)
            query = query.Where(x => x.UserId == userId.Value);

        if (!string.IsNullOrWhiteSpace(request.Search))
        {
            var search = request.Search.Trim().ToLower();
            query = query.Where(x =>
                x.EntityType.ToLower().Contains(search) ||
                x.Description.ToLower().Contains(search));
        }

        if (request.Filters is not null)
        {
            if (request.Filters.TryGetValue("action", out var actionFilter) &&
                Enum.TryParse<AuditAction>(actionFilter, true, out var parsedAction))
            {
                query = query.Where(x => x.Action == parsedAction);
            }

            if (request.Filters.TryGetValue("entityType", out var entityTypeFilter) &&
                !string.IsNullOrWhiteSpace(entityTypeFilter))
            {
                query = query.Where(x => x.EntityType.ToLower() == entityTypeFilter.Trim().ToLower());
            }

            if (request.Filters.TryGetValue("from", out var fromStr) &&
                DateTime.TryParse(fromStr, out var from))
            {
                query = query.Where(x => x.DateCreated >= from);
            }

            if (request.Filters.TryGetValue("to", out var toStr) &&
                DateTime.TryParse(toStr, out var to))
            {
                query = query.Where(x => x.DateCreated <= to);
            }
        }

        var totalCount = await query.CountAsync(cancellationToken);

        var items = await query
            .OrderByDescending(x => x.Id)
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .Select(x => new AuditLogResponse
            {
                Id = x.Id,
                UserId = x.UserId,
                UserName = x.User.FullName,
                BusinessId = x.BusinessId,
                Action = x.Action.ToString(),
                EntityType = x.EntityType,
                EntityId = x.EntityId,
                Description = x.Description,
                OldValues = x.OldValues,
                NewValues = x.NewValues,
                IpAddress = x.IpAddress,
                Timestamp = x.DateCreated
            })
            .ToListAsync(cancellationToken);

        var paged = new PagedResponse<AuditLogResponse>
        {
            PageNumber = pageNumber,
            PageSize = pageSize,
            TotalCount = totalCount,
            TotalPages = (int)Math.Ceiling(totalCount / (double)pageSize),
            Items = items
        };

        return ApiResponse<PagedResponse<AuditLogResponse>>.Ok(paged);
    }
}
