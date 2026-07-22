using AutVent.Application.Contracts;
using AutVent.Shared.Results;

namespace AutVent.Application.Abstractions.Services;

public interface IAuthenticationService
{
    Task<Result<AuthSessionDto>> LoginAsync(LoginRequest request, CancellationToken cancellationToken);

    Task<Result<AuthSessionDto>> TryOfflineLoginAsync(string email, CancellationToken cancellationToken);

    Task<Result> LogoutAsync(CancellationToken cancellationToken);
}
