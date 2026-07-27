using AutVent.CorePlatform.Api.Common;
using AutVent.CorePlatform.Api.Common.Email;
using AutVent.CorePlatform.Api.Common.Requests;
using AutVent.CorePlatform.Api.Common.Responses;
using AutVent.CorePlatform.Api.Infrastructure.Email;
using AutVent.CorePlatform.Domain.Entities;
using AutVent.CorePlatform.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace AutVent.CorePlatform.Api.Services;

public sealed class WaitlistService(IUnitOfWork unitOfWork, IEmailProvider emailProvider, IOptions<EmailOptions> emailOptions, IOptions<AppOptions> appOptions) : IWaitlistService
{
    private const string SystemActor = "system";

    public async Task<ApiResponse<WaitlistResponse>> JoinAsync(JoinWaitlistRequest request, CancellationToken cancellationToken = default)
    {
        var normalizedEmail = request.EmailAddress.Trim().ToLowerInvariant();

        var alreadyJoined = await unitOfWork.Query<WaitlistEntry>()
            .AnyAsync(x => x.EmailAddress == normalizedEmail, cancellationToken);

        if (alreadyJoined)
        {
            return ApiResponse<WaitlistResponse>.Failed(
                StatusCodes.Status409Conflict,
                "This email address is already on the waitlist",
                [new ApiError("DuplicateEmail", "Email address already registered on the waitlist", nameof(request.EmailAddress))]);
        }

        var now = DateTime.UtcNow;

        var entry = new WaitlistEntry
        {
            FullName = request.FullName.Trim(),
            EmailAddress = normalizedEmail,
            PhoneNumber = request.PhoneNumber?.Trim(),
            BusinessType = request.BusinessType?.Trim(),
            Notes = request.Notes?.Trim(),
            IsContacted = false,
            IsActive = true,
            CreatedBy = SystemActor,
            DateCreated = now
        };

        await unitOfWork.CreateAsync(entry, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        await emailProvider.SendAsync(
            EmailTemplates.WaitlistConfirmation(entry.EmailAddress, entry.FullName, appOptions.Value.AutVentLoginUrl, emailOptions),
            cancellationToken);

        return ApiResponse<WaitlistResponse>.Created(Map(entry), "You have been added to the waitlist successfully");
    }

    public async Task<ApiResponse<PagedResponse<WaitlistResponse>>> GetAllAsync(PagedQueryRequest request, CancellationToken cancellationToken = default)
    {
        var pageNumber = Math.Max(1, request.PageNumber);
        var pageSize = Math.Clamp(request.PageSize, 1, 100);

        var query = unitOfWork.Query<WaitlistEntry>()
            .Where(x => !x.IsDeleted)
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(request.Search))
        {
            var search = request.Search.Trim().ToLower();
            query = query.Where(x =>
                x.FullName.ToLower().Contains(search) ||
                x.EmailAddress.ToLower().Contains(search) ||
                (x.BusinessType != null && x.BusinessType.ToLower().Contains(search)));
        }

        if (request.Filters is not null)
        {
            if (request.Filters.TryGetValue("isContacted", out var isContactedStr) &&
                bool.TryParse(isContactedStr, out var isContacted))
            {
                query = query.Where(x => x.IsContacted == isContacted);
            }
        }

        var totalCount = await query.CountAsync(cancellationToken);

        var items = await query
            .OrderByDescending(x => x.Id)
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .Select(x => new WaitlistResponse
            {
                Id = x.Id,
                FullName = x.FullName,
                EmailAddress = x.EmailAddress,
                PhoneNumber = x.PhoneNumber,
                BusinessType = x.BusinessType,
                Notes = x.Notes,
                IsContacted = x.IsContacted,
                JoinedAt = x.DateCreated
            })
            .ToListAsync(cancellationToken);

        var paged = new PagedResponse<WaitlistResponse>
        {
            PageNumber = pageNumber,
            PageSize = pageSize,
            TotalCount = totalCount,
            TotalPages = (int)Math.Ceiling(totalCount / (double)pageSize),
            Items = items
        };

        return ApiResponse<PagedResponse<WaitlistResponse>>.Ok(paged);
    }

    public async Task<ApiResponse<bool>> MarkContactedAsync(long id, CancellationToken cancellationToken = default)
    {
        var entry = await unitOfWork.Query<WaitlistEntry>()
            .FirstOrDefaultAsync(x => x.Id == id && !x.IsDeleted, cancellationToken);

        if (entry is null)
        {
            return ApiResponse<bool>.Failed(
                StatusCodes.Status404NotFound,
                "Waitlist entry not found",
                [new ApiError("NotFound", "No waitlist entry found for this id", nameof(id))]);
        }

        if (entry.IsContacted)
            return ApiResponse<bool>.Ok(true, "Entry is already marked as contacted");

        entry.IsContacted = true;
        entry.UpdatedBy = SystemActor;
        entry.DateUpdated = DateTime.UtcNow;

        unitOfWork.Update(entry);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return ApiResponse<bool>.Ok(true, "Entry marked as contacted");
    }

    public async Task<ApiResponse<bool>> DeleteAsync(long id, CancellationToken cancellationToken = default)
    {
        var entry = await unitOfWork.Query<WaitlistEntry>()
            .FirstOrDefaultAsync(x => x.Id == id && !x.IsDeleted, cancellationToken);

        if (entry is null)
        {
            return ApiResponse<bool>.Failed(
                StatusCodes.Status404NotFound,
                "Waitlist entry not found",
                [new ApiError("NotFound", "No waitlist entry found for this id", nameof(id))]);
        }

        var now = DateTime.UtcNow;
        entry.IsDeleted = true;
        entry.IsActive = false;
        entry.DateDeleted = now;
        entry.DateUpdated = now;
        entry.UpdatedBy = SystemActor;

        unitOfWork.Update(entry);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return ApiResponse<bool>.Ok(true, "Waitlist entry removed successfully");
    }

    private static WaitlistResponse Map(WaitlistEntry entry) => new()
    {
        Id = entry.Id,
        FullName = entry.FullName,
        EmailAddress = entry.EmailAddress,
        PhoneNumber = entry.PhoneNumber,
        BusinessType = entry.BusinessType,
        Notes = entry.Notes,
        IsContacted = entry.IsContacted,
        JoinedAt = entry.DateCreated
    };
}
