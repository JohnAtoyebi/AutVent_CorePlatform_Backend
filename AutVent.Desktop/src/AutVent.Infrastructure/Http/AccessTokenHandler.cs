using System.Net;
using System.Net.Http.Headers;
using AutVent.Application.Abstractions.Api;
using AutVent.Application.Abstractions.Persistence;
using AutVent.Application.Contracts;
using AutVent.Application.Abstractions.Security;

namespace AutVent.Infrastructure.Http;

public sealed class AccessTokenHandler : DelegatingHandler
{
    private readonly ITokenStore _tokenStore;
    private readonly IAuthSessionRepository _authSessionRepository;
    private readonly IAuthApiClient _authApiClient;

    public AccessTokenHandler(ITokenStore tokenStore, IAuthSessionRepository authSessionRepository, IAuthApiClient authApiClient)
    {
        _tokenStore = tokenStore;
        _authSessionRepository = authSessionRepository;
        _authApiClient = authApiClient;
    }

    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        if (!string.IsNullOrWhiteSpace(_tokenStore.AccessToken))
        {
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _tokenStore.AccessToken);
        }

        var response = await base.SendAsync(request, cancellationToken);

        if (response.StatusCode is not HttpStatusCode.Unauthorized)
        {
            return response;
        }

        var session = await _authSessionRepository.GetCurrentSessionAsync(cancellationToken);
        if (session is null || session.RefreshTokenExpiresAtUtc <= DateTime.UtcNow)
        {
            return response;
        }

        var refreshed = await _authApiClient.RefreshAsync(new RefreshTokenRequest(session.RefreshToken), cancellationToken);
        _tokenStore.AccessToken = refreshed.AccessToken;
        _tokenStore.RefreshToken = refreshed.RefreshToken;

        // Persist the updated session so the new refresh token survives restarts
        session.AccessToken = refreshed.AccessToken;
        session.RefreshToken = refreshed.RefreshToken;
        session.RefreshTokenExpiresAtUtc = refreshed.RefreshTokenExpiresAtUtc;
        await _authSessionRepository.SaveSessionAsync(session, cancellationToken);

        response.Dispose();

        var retryRequest = await CloneAsync(request, cancellationToken);
        retryRequest.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _tokenStore.AccessToken);

        return await base.SendAsync(retryRequest, cancellationToken);
    }

    private static async Task<HttpRequestMessage> CloneAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        var clone = new HttpRequestMessage(request.Method, request.RequestUri);

        foreach (var header in request.Headers)
        {
            clone.Headers.TryAddWithoutValidation(header.Key, header.Value);
        }

        if (request.Content is not null)
        {
            var ms = new MemoryStream();
            await request.Content.CopyToAsync(ms, cancellationToken);
            ms.Position = 0;
            clone.Content = new StreamContent(ms);

            foreach (var header in request.Content.Headers)
            {
                clone.Content.Headers.Add(header.Key, header.Value);
            }
        }

        return clone;
    }
}
