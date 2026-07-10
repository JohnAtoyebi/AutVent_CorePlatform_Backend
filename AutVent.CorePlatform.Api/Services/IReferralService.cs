using AutVent.CorePlatform.Api.Common.Responses;

namespace AutVent.CorePlatform.Api.Services;

public interface IReferralService
{
    Task<ApiResponse<ValidateReferralCodeResponse>> ValidateReferralCodeAsync(string referralCode, CancellationToken cancellationToken = default);
}
