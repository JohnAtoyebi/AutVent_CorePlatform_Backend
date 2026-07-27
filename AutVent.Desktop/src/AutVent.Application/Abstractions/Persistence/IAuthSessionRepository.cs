using AutVent.Domain.Entities;

namespace AutVent.Application.Abstractions.Persistence;

public interface IAuthSessionRepository
{
    Task<AuthenticationSession?> GetCurrentSessionAsync(CancellationToken cancellationToken);

    Task SaveSessionAsync(AuthenticationSession session, CancellationToken cancellationToken);

    Task DeleteSessionAsync(CancellationToken cancellationToken);
}
