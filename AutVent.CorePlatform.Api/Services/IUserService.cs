using AutVent.CorePlatform.Api.Common.Requests;
using AutVent.CorePlatform.Api.Common.Responses;

namespace AutVent.CorePlatform.Api.Services;

public interface IUserService
{
    Task<ApiResponse<UserProfileResponse>> GetCurrentUserAsync(long userId, CancellationToken cancellationToken = default);
    Task<ApiResponse<UpdateProfileResponse>> UpdateProfileAsync(long userId, UpdateProfileRequest request, CancellationToken cancellationToken = default);
    Task<ApiResponse<ChangePasswordResponse>> ChangePasswordAsync(long userId, ChangePasswordRequest request, CancellationToken cancellationToken = default);
    Task<ApiResponse<ChangeEmailResponse>> ChangeEmailAsync(long userId, ChangeEmailRequest request, CancellationToken cancellationToken = default);
}
