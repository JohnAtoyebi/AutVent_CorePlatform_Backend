using AutVent.Application.Abstractions.Persistence;
using AutVent.Application.Abstractions.Security;
using AutVent.Domain.Entities;
using AutVent.Infrastructure.Data;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.EntityFrameworkCore;

namespace AutVent.Infrastructure.Persistence;

public sealed class AuthSessionRepository : IAuthSessionRepository
{
    private readonly AutVentDbContext _dbContext;
    private readonly IDataProtector _protector;
    private readonly ITokenStore _tokenStore;

    public AuthSessionRepository(AutVentDbContext dbContext, IDataProtectionProvider dataProtectionProvider, ITokenStore tokenStore)
    {
        _dbContext = dbContext;
        _tokenStore = tokenStore;
        _protector = dataProtectionProvider.CreateProtector("AutVent.Desktop.AuthSession");
    }

    public async Task<AuthenticationSession?> GetCurrentSessionAsync(CancellationToken cancellationToken)
    {
        var existing = await _dbContext.AuthenticationSessions.OrderByDescending(x => x.CreatedAtUtc).FirstOrDefaultAsync(cancellationToken);
        if (existing is null)
        {
            return null;
        }

        existing.AccessToken = _protector.Unprotect(existing.AccessToken);
        existing.RefreshToken = _protector.Unprotect(existing.RefreshToken);

        _tokenStore.AccessToken = existing.AccessToken;
        _tokenStore.RefreshToken = existing.RefreshToken;

        return existing;
    }

    public async Task SaveSessionAsync(AuthenticationSession session, CancellationToken cancellationToken)
    {
        var rows = await _dbContext.AuthenticationSessions.ToListAsync(cancellationToken);
        _dbContext.AuthenticationSessions.RemoveRange(rows);

        _tokenStore.AccessToken = session.AccessToken;
        _tokenStore.RefreshToken = session.RefreshToken;

        var copy = new AuthenticationSession
        {
            Id = session.Id,
            UserId = session.UserId,
            FullName = session.FullName,
            Email = session.Email,
            AccessToken = _protector.Protect(session.AccessToken),
            RefreshToken = _protector.Protect(session.RefreshToken),
            AccessTokenExpiresAtUtc = session.AccessTokenExpiresAtUtc,
            RefreshTokenExpiresAtUtc = session.RefreshTokenExpiresAtUtc,
            CreatedAtUtc = session.CreatedAtUtc
        };

        _dbContext.AuthenticationSessions.Add(copy);
    }

    public async Task DeleteSessionAsync(CancellationToken cancellationToken)
    {
        var rows = await _dbContext.AuthenticationSessions.ToListAsync(cancellationToken);
        _dbContext.AuthenticationSessions.RemoveRange(rows);
        _tokenStore.AccessToken = null;
        _tokenStore.RefreshToken = null;
    }
}
