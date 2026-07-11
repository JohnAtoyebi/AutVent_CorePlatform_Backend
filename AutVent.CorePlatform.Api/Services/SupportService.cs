using AutVent.CorePlatform.Api.Common.Email;
using AutVent.CorePlatform.Api.Common.Requests;
using AutVent.CorePlatform.Api.Common.Responses;
using AutVent.CorePlatform.Api.Infrastructure.Email;
using AutVent.CorePlatform.Domain.Entities;
using AutVent.CorePlatform.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace AutVent.CorePlatform.Api.Services;

public sealed class SupportService(
    IUnitOfWork unitOfWork,
    IEmailProvider emailProvider,
    IOptions<EmailOptions> emailOptions) : ISupportService
{
    public async Task<ApiResponse<string>> ContactAsync(ContactSupportRequest request, CancellationToken cancellationToken = default)
    {
        var supportEmail = emailOptions.Value.SupportEmail;

        if (string.IsNullOrWhiteSpace(supportEmail))
            return ApiResponse<string>.Failed(
                StatusCodes.Status503ServiceUnavailable,
                "Support contact is not configured.");

        var supportRequest = new SupportRequest
        {
            FullName = request.FullName,
            Email = request.Email,
            Message = request.Message,
            IsActive = true,
            IsResolved = false,
            CreatedBy = request.Email,
            DateCreated = DateTime.UtcNow
        };

        await unitOfWork.CreateAsync(supportRequest, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        await emailProvider.SendAsync(
            EmailTemplates.ContactSupport(
                supportEmail,
                request.FullName,
                request.Email,
                request.Message,
                emailOptions),
            cancellationToken);

        return ApiResponse<string>.Ok("Your message has been received. We will get back to you shortly.");
    }

    public async Task<ApiResponse<PagedResponse<SupportRequestResponse>>> GetAllAsync(PagedQueryRequest request, CancellationToken cancellationToken = default)
    {
        var query = unitOfWork.Query<SupportRequest>()
            .Where(x => !x.IsDeleted);

        if (!string.IsNullOrWhiteSpace(request.Search))
        {
            var search = request.Search.Trim().ToLower();
            query = query.Where(x =>
                x.FullName.ToLower().Contains(search) ||
                x.Email.ToLower().Contains(search));
        }

        if (request.Filters is not null &&
            request.Filters.TryGetValue("isResolved", out var isResolvedFilter) &&
            bool.TryParse(isResolvedFilter, out var isResolved))
        {
            query = query.Where(x => x.IsResolved == isResolved);
        }

        var totalCount = await query.CountAsync(cancellationToken);

        query = request.SortBy?.ToLower() switch
        {
            "oldest" => query.OrderBy(x => x.Id),
            _ => query.OrderByDescending(x => x.Id)
        };

        var items = await query
            .Skip((request.PageNumber - 1) * request.PageSize)
            .Take(request.PageSize)
            .Select(x => new SupportRequestResponse
            {
                Id = x.Id,
                FullName = x.FullName,
                Email = x.Email,
                Message = x.Message,
                IsResolved = x.IsResolved,
                DateCreated = x.DateCreated,
                DateUpdated = x.DateUpdated
            })
            .ToListAsync(cancellationToken);

        var paged = new PagedResponse<SupportRequestResponse>
        {
            PageNumber = request.PageNumber,
            PageSize = request.PageSize,
            TotalCount = totalCount,
            TotalPages = (int)Math.Ceiling(totalCount / (double)request.PageSize),
            Items = items
        };

        return ApiResponse<PagedResponse<SupportRequestResponse>>.Ok(paged);
    }
}
