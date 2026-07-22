using AutVent.Application.Contracts;

namespace AutVent.Application.Abstractions.Api;

public interface IAuthApiClient
{
    Task<LoginResponse> LoginAsync(LoginRequest request, CancellationToken cancellationToken);

    Task<RefreshTokenResponse> RefreshAsync(RefreshTokenRequest request, CancellationToken cancellationToken);
}
