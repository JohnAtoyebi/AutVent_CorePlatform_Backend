using System.Net.Http.Json;
using System.Text.Json.Serialization;
using AutVent.Application.Abstractions.Api;
using AutVent.Application.Contracts;

namespace AutVent.Infrastructure.Api;

// ── Internal API shapes (match server swagger schemas) ──────────────────────

file sealed record SignInRequestBody(
    [property: JsonPropertyName("emailAddress")] string EmailAddress,
    [property: JsonPropertyName("password")] string Password);

file sealed record SignInResponseBody(
    [property: JsonPropertyName("userId")] long UserId,
    [property: JsonPropertyName("fullName")] string FullName,
    [property: JsonPropertyName("emailAddress")] string EmailAddress,
    [property: JsonPropertyName("accessToken")] string AccessToken,
    [property: JsonPropertyName("accessTokenExpiresAtUtc")] DateTime AccessTokenExpiresAtUtc,
    [property: JsonPropertyName("refreshToken")] string RefreshToken,
    [property: JsonPropertyName("refreshTokenExpiresAtUtc")] DateTime RefreshTokenExpiresAtUtc,
    [property: JsonPropertyName("isBusinessCreated")] bool IsBusinessCreated);

file sealed record RefreshRequestBody(
    [property: JsonPropertyName("refreshToken")] string RefreshToken);

file sealed record RefreshResponseBody(
    [property: JsonPropertyName("accessToken")] string AccessToken,
    [property: JsonPropertyName("refreshToken")] string RefreshToken,
    [property: JsonPropertyName("refreshTokenExpiresAtUtc")] DateTime RefreshTokenExpiresAtUtc);

// ── Client ───────────────────────────────────────────────────────────────────

public sealed class AuthApiClient : ApiClientBase, IAuthApiClient
{
    private readonly HttpClient _httpClient;

    public AuthApiClient(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    public async Task<LoginResponse> LoginAsync(LoginRequest request, CancellationToken cancellationToken)
    {
        var body = new SignInRequestBody(request.Email, request.Password);
        var response = await _httpClient.PostAsJsonAsync("api/Authentication/sign-in", body, JsonOptions, cancellationToken);
        var data = await ReadApiResponseAsync<SignInResponseBody>(response, cancellationToken);

        return new LoginResponse(
            UserId: data.UserId,
            FullName: data.FullName,
            Email: data.EmailAddress,
            AccessToken: data.AccessToken,
            RefreshToken: data.RefreshToken,
            AccessTokenExpiresAtUtc: data.AccessTokenExpiresAtUtc,
            RefreshTokenExpiresAtUtc: data.RefreshTokenExpiresAtUtc,
            IsBusinessCreated: data.IsBusinessCreated);
    }

    public async Task<RefreshTokenResponse> RefreshAsync(RefreshTokenRequest request, CancellationToken cancellationToken)
    {
        var body = new RefreshRequestBody(request.RefreshToken);
        var response = await _httpClient.PostAsJsonAsync("api/Authentication/refresh-token", body, JsonOptions, cancellationToken);
        var data = await ReadApiResponseAsync<RefreshResponseBody>(response, cancellationToken);

        return new RefreshTokenResponse(
            AccessToken: data.AccessToken,
            RefreshToken: data.RefreshToken,
            RefreshTokenExpiresAtUtc: data.RefreshTokenExpiresAtUtc);
    }
}
