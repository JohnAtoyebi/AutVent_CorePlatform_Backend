using AutVent.CorePlatform.Api.Common.Responses;
using AutVent.CorePlatform.Domain.Entities;
using AutVent.CorePlatform.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace AutVent.CorePlatform.Api.Services;

public sealed class ReferralService(IUnitOfWork unitOfWork) : IReferralService
{
    public async Task<ApiResponse<ValidateReferralCodeResponse>> ValidateReferralCodeAsync(string referralCode, CancellationToken cancellationToken = default)
    {
        var normalized = referralCode.Trim().ToUpperInvariant();

        var referrer = await unitOfWork.Query<User>()
            .FirstOrDefaultAsync(x => x.ReferralCode == normalized, cancellationToken);

        if (referrer is null)
        {
            return ApiResponse<ValidateReferralCodeResponse>.Failed(
                StatusCodes.Status404NotFound,
                "Referral code not found",
                [new ApiError("InvalidReferralCode", "The referral code provided is not valid", nameof(referralCode))]);
        }

        var response = new ValidateReferralCodeResponse
        {
            ReferralCode = normalized,
            IsValid = true,
            ReferrerName = referrer.FullName
        };

        return ApiResponse<ValidateReferralCodeResponse>.Ok(response, "Referral code is valid");
    }
}
