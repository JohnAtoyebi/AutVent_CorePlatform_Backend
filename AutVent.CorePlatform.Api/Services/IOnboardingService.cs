using AutVent.CorePlatform.Api.Common.Requests;
using AutVent.CorePlatform.Api.Common.Responses;

namespace AutVent.CorePlatform.Api.Services;

public interface IOnboardingService
{
    Task<ApiResponse<RegisterUserResponse>> RegisterAsync(RegisterUserRequest request, CancellationToken cancellationToken = default);
    Task<ApiResponse<VerifyOtpResponse>> VerifyOtpAsync(VerifyOtpRequest request, CancellationToken cancellationToken = default);
    Task<ApiResponse<ResendOtpResponse>> ResendOtpAsync(ResendOtpRequest request, CancellationToken cancellationToken = default);
}
