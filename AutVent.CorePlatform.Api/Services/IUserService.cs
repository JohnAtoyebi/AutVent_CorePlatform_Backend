using AutVent.CorePlatform.Api.Common.Responses;

namespace AutVent.CorePlatform.Api.Services;

public interface IUserService
{
    Task<ApiResponse<UserProfileResponse>> GetCurrentUserAsync(long userId, CancellationToken cancellationToken = default);
}
