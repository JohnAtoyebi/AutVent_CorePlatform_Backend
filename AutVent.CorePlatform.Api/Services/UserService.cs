using AutVent.CorePlatform.Api.Common.Responses;
using AutVent.CorePlatform.Domain.Entities;
using AutVent.CorePlatform.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace AutVent.CorePlatform.Api.Services;

public sealed class UserService(IUnitOfWork unitOfWork) : IUserService
{
    public async Task<ApiResponse<UserProfileResponse>> GetCurrentUserAsync(long userId, CancellationToken cancellationToken = default)
    {
        var user = await unitOfWork.Query<User>()
            .FirstOrDefaultAsync(x => x.Id == userId, cancellationToken);

        if (user is null)
        {
            return ApiResponse<UserProfileResponse>.Failed(
                StatusCodes.Status404NotFound,
                "User not found",
                [new ApiError("UserNotFound", "No user found for this id", "userId")]);
        }

        var response = new UserProfileResponse
        {
            Id = user.Id,
            FullName = user.FullName,
            EmailAddress = user.EmailAddress,
            PhoneNumber = user.PhoneNumber,
            ReferralCode = user.ReferralCode,
            IsActive = user.IsActive,
            MemberSince = user.DateCreated
        };

        return ApiResponse<UserProfileResponse>.Ok(response);
    }
}
