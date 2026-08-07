using AutVent.CorePlatform.Api.Common.Requests;
using AutVent.CorePlatform.Api.Common.Responses;

namespace AutVent.CorePlatform.Api.Services;

public interface IReferralService
{
    Task<ApiResponse<ValidateReferralCodeResponse>> ValidateReferralCodeAsync(string referralCode, CancellationToken cancellationToken = default);

    /// <summary>
    /// Get businesses that were referred by a specific user, including their subscription details
    /// </summary>
    /// <param name="referrerId">The ID of the user who did the referring</param>
    /// <param name="request">Pagination and filtering request</param>
    /// <param name="userId">The current user ID (for authorization)</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Paged list of referred businesses with subscription status</returns>
    Task<ApiResponse<PagedResponse<ReferredBusinessResponse>>> GetReferredBusinessesAsync(
        long referrerId,
        PagedQueryRequest request,
        long userId,
        CancellationToken cancellationToken = default);
}
