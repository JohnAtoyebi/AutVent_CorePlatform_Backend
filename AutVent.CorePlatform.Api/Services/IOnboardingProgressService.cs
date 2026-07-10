using AutVent.CorePlatform.Api.Common.Responses;

namespace AutVent.CorePlatform.Api.Services;

public interface IOnboardingProgressService
{
    Task<ApiResponse<OnboardingProgressResponse>> GetProgressAsync(long userId, CancellationToken cancellationToken = default);
}
