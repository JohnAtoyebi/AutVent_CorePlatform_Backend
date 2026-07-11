using AutVent.CorePlatform.Api.Common.Email;
using AutVent.CorePlatform.Api.Common.Requests;
using AutVent.CorePlatform.Api.Common.Responses;
using AutVent.CorePlatform.Api.Infrastructure.Email;
using AutVent.CorePlatform.Domain.Entities;
using AutVent.CorePlatform.Infrastructure.Persistence;
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
}
