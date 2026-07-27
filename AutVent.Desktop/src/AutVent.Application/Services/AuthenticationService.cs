using AutVent.Application.Abstractions.Api;
using AutVent.Application.Abstractions.Persistence;
using AutVent.Application.Abstractions.Security;
using AutVent.Application.Abstractions.Services;
using AutVent.Application.Abstractions.System;
using AutVent.Application.Contracts;
using AutVent.Domain.Entities;
using AutVent.Shared.Results;

namespace AutVent.Application.Services;

public sealed class AuthenticationService : IAuthenticationService
{
    private readonly IAuthApiClient _authApiClient;
    private readonly ICatalogApiClient _catalogApiClient;
    private readonly IProductRepository _productRepository;
    private readonly IStoreRepository _storeRepository;
    private readonly IAuthSessionRepository _authSessionRepository;
    private readonly IConnectivityService _connectivityService;
    private readonly ITokenStore _tokenStore;
    private readonly IUnitOfWork _unitOfWork;

    public AuthenticationService(
        IAuthApiClient authApiClient,
        ICatalogApiClient catalogApiClient,
        IProductRepository productRepository,
        IStoreRepository storeRepository,
        IAuthSessionRepository authSessionRepository,
        IConnectivityService connectivityService,
        ITokenStore tokenStore,
        IUnitOfWork unitOfWork)
    {
        _authApiClient = authApiClient;
        _catalogApiClient = catalogApiClient;
        _productRepository = productRepository;
        _storeRepository = storeRepository;
        _authSessionRepository = authSessionRepository;
        _connectivityService = connectivityService;
        _tokenStore = tokenStore;
        _unitOfWork = unitOfWork;
    }

    public async Task<Result<AuthSessionDto>> LoginAsync(LoginRequest request, CancellationToken cancellationToken)
    {
        if (!_connectivityService.IsOnline())
        {
            return Result<AuthSessionDto>.Failure("Internet connection is required for first login.");
        }

        var loginResponse = await _authApiClient.LoginAsync(request, cancellationToken);

        // Seed the in-memory token cache immediately so all subsequent authenticated
        // requests (catalog, inventory, POS) carry the bearer token without a 401.
        _tokenStore.AccessToken = loginResponse.AccessToken;
        _tokenStore.RefreshToken = loginResponse.RefreshToken;

        // 1. Fetch and persist stores first (we need remoteStoreId for products)
        var stores = await _catalogApiClient.GetStoresAsync(cancellationToken);
        await _storeRepository.UpsertAsync(stores, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        // 2. Fetch products for each store
        foreach (var store in stores)
        {
            var products = await _catalogApiClient.GetProductsAsync(store.RemoteId, cancellationToken);
            await _productRepository.UpsertAsync(products, cancellationToken);
        }
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        var session = new AuthenticationSession
        {
            Id = Guid.NewGuid(),
            UserId = loginResponse.UserId,
            FullName = loginResponse.FullName,
            Email = loginResponse.Email,
            AccessToken = loginResponse.AccessToken,
            RefreshToken = loginResponse.RefreshToken,
            AccessTokenExpiresAtUtc = loginResponse.AccessTokenExpiresAtUtc,
            RefreshTokenExpiresAtUtc = loginResponse.RefreshTokenExpiresAtUtc,
            CreatedAtUtc = DateTime.UtcNow
        };

        await _authSessionRepository.SaveSessionAsync(session, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        var dto = new AuthSessionDto(
            session.UserId,
            session.FullName,
            session.Email,
            session.AccessToken,
            session.RefreshToken,
            session.AccessTokenExpiresAtUtc,
            session.RefreshTokenExpiresAtUtc);

        return Result<AuthSessionDto>.Success(dto);
    }

    public async Task<Result<AuthSessionDto>> TryOfflineLoginAsync(string email, CancellationToken cancellationToken)
    {
        var session = await _authSessionRepository.GetCurrentSessionAsync(cancellationToken);
        if (session is null)
        {
            return Result<AuthSessionDto>.Failure("No cached session found. Internet login is required.");
        }

        if (!string.Equals(session.Email, email, StringComparison.OrdinalIgnoreCase))
        {
            return Result<AuthSessionDto>.Failure("Cached session does not match this account.");
        }

        if (session.RefreshTokenExpiresAtUtc <= DateTime.UtcNow)
        {
            return Result<AuthSessionDto>.Failure("Cached session expired. Internet login is required.");
        }

        // Restore the in-memory token cache from the persisted session.
        _tokenStore.AccessToken = session.AccessToken;
        _tokenStore.RefreshToken = session.RefreshToken;

        return Result<AuthSessionDto>.Success(new AuthSessionDto(
            session.UserId,
            session.FullName,
            session.Email,
            session.AccessToken,
            session.RefreshToken,
            session.AccessTokenExpiresAtUtc,
            session.RefreshTokenExpiresAtUtc));
    }

    public async Task<Result> LogoutAsync(CancellationToken cancellationToken)
    {
        // Clear the in-memory token cache so no further requests carry a stale bearer token.
        _tokenStore.AccessToken = null;
        _tokenStore.RefreshToken = null;

        // No server-side logout endpoint — clear the local session only
        await _authSessionRepository.DeleteSessionAsync(cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }
}
