using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using AutVent.CorePlatform.Api.Common;
using AutVent.CorePlatform.Api.Common.Email;
using AutVent.CorePlatform.Api.Common.Requests;
using AutVent.CorePlatform.Api.Common.Responses;
using AutVent.CorePlatform.Api.Common.Security;
using AutVent.CorePlatform.Api.Infrastructure.Email;
using AutVent.CorePlatform.Domain.Entities;
using AutVent.CorePlatform.Domain.Enums;
using AutVent.CorePlatform.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace AutVent.CorePlatform.Api.Services;

public sealed class AuthenticationService(
    IUnitOfWork unitOfWork,
    IConfiguration configuration,
    IEmailProvider emailProvider,
    IOptions<EmailOptions> emailOptions,
    IOptions<AppOptions> appOptions,
    IJwtTokenService jwtTokenService,
    IAuditLogService auditLogService) : IAuthenticationService
{
    private const string SystemActor = "system";

    public async Task<ApiResponse<SignInResponse>> SignInAsync(SignInRequest request, CancellationToken cancellationToken = default)
    {
        var normalizedEmail = request.EmailAddress.Trim().ToLowerInvariant();
        var hashedPassword = PasswordHasher.Hash(request.Password);

        var user = await unitOfWork.Query<User>()
            .FirstOrDefaultAsync(x => x.EmailAddress.ToLower() == normalizedEmail, cancellationToken);

        if (user is null || !string.Equals(user.Password, hashedPassword, StringComparison.Ordinal))
        {
            return ApiResponse<SignInResponse>.Failed(
                StatusCodes.Status401Unauthorized,
                "Invalid credentials",
                [new ApiError("InvalidCredentials", "Email or password is incorrect")]);
        }

        if (!user.IsActive)
        {
            return ApiResponse<SignInResponse>.Failed(
                StatusCodes.Status403Forbidden,
                "Email is not verified",
                [new ApiError("EmailNotVerified", "Verify your email before signing in", nameof(request.EmailAddress))]);
        }

        var business = await unitOfWork.Query<Business>()
            .FirstOrDefaultAsync(x => x.UserId == user.Id, cancellationToken);

        var (rawRefreshToken, refreshExpiresAt) = jwtTokenService.GenerateRefreshToken();

        var refreshToken = new RefreshToken
        {
            UserId = user.Id,
            Token = rawRefreshToken,
            DateExpired = refreshExpiresAt,
            IsUsed = false,
            IsRevoked = false,
            IsActive = true,
            CreatedBy = SystemActor,
            DateCreated = DateTime.UtcNow
        };

        await unitOfWork.CreateAsync(refreshToken, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        await auditLogService.LogAsync(
            user.Id,
            AuditAction.UserSignedIn,
            nameof(User),
            $"User '{user.EmailAddress}' signed in.",
            entityId: user.Id,
            cancellationToken: cancellationToken);

        var (accessToken, accessExpiresAt) = jwtTokenService.GenerateAccessTokenWithExpiry(user);

        var response = new SignInResponse
        {
            UserId = user.Id,
            FullName = user.FullName,
            EmailAddress = user.EmailAddress,
            AccessToken = accessToken,
            AccessTokenExpiresAtUtc = accessExpiresAt,
            RefreshToken = rawRefreshToken,
            RefreshTokenExpiresAtUtc = refreshExpiresAt,
            IsBusinessCreated = business == null ? false : true
        };

        return ApiResponse<SignInResponse>.Ok(response, "Sign in successful");
    }

    public async Task<ApiResponse<ForgotPasswordResponse>> SendResetLinkAsync(ForgotPasswordRequest request, CancellationToken cancellationToken = default)
    {
        var normalizedEmail = request.EmailAddress.Trim().ToLowerInvariant();
        var now = DateTime.UtcNow;

        var resetBaseUrl = appOptions.Value.PasswordResetBaseUrl;

        if (string.IsNullOrWhiteSpace(resetBaseUrl))
        {
            throw new InvalidOperationException("PasswordResetBaseUrl is not configured.");
        }

        var user = await unitOfWork.Query<User>()
            .FirstOrDefaultAsync(x => x.EmailAddress.ToLower() == normalizedEmail, cancellationToken);

        var silentResponse = new ForgotPasswordResponse { EmailAddress = normalizedEmail };

        if (user is null || !user.IsActive)
        {
            return ApiResponse<ForgotPasswordResponse>.Ok(silentResponse, "If that email is registered, a reset link has been sent");
        }

        var activeTokens = await unitOfWork.Query<PasswordResetToken>()
            .Where(x => x.UserId == user.Id && !x.IsUsed)
            .ToListAsync(cancellationToken);

        foreach (var active in activeTokens)
        {
            active.IsUsed = true;
            active.IsActive = false;
            active.DateUpdated = now;
            active.UpdatedBy = SystemActor;
            unitOfWork.Update(active);
        }

        var rawToken = Convert.ToBase64String(RandomNumberGenerator.GetBytes(64));
        var expiresAt = now.AddMinutes(appOptions.Value.PasswordResetExpiryMinutes);

        var resetToken = new PasswordResetToken
        {
            UserId = user.Id,
            Token = rawToken,
            DateExpired = expiresAt,
            IsUsed = false,
            IsActive = true,
            CreatedBy = SystemActor,
            DateCreated = now
        };

        await unitOfWork.CreateAsync(resetToken, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        var resetLink = $"{resetBaseUrl.TrimEnd('/')}?token={Uri.EscapeDataString(rawToken)}";

        await emailProvider.SendAsync(
            EmailTemplates.ForgotPassword(normalizedEmail, user.FullName, resetLink, expiresAt, emailOptions),
            cancellationToken);

        var response = new ForgotPasswordResponse
        {
            EmailAddress = normalizedEmail,
            TokenExpiresAtUtc = expiresAt
        };

        return ApiResponse<ForgotPasswordResponse>.Ok(response, "If that email is registered, a reset link has been sent");
    }

    public Task<ApiResponse<ForgotPasswordResponse>> ResendResetLinkAsync(ForgotPasswordRequest request, CancellationToken cancellationToken = default)
        => SendResetLinkAsync(request, cancellationToken);

    public async Task<ApiResponse<ResetPasswordResponse>> ResetPasswordAsync(ResetPasswordRequest request, CancellationToken cancellationToken = default)
    {
        var now = DateTime.UtcNow;

        var resetToken = await unitOfWork.Query<PasswordResetToken>()
            .Include(x => x.User)
            .FirstOrDefaultAsync(x => x.Token == request.Token, cancellationToken);

        if (resetToken is null)
        {
            return ApiResponse<ResetPasswordResponse>.Failed(
                StatusCodes.Status400BadRequest,
                "Invalid reset token",
                [new ApiError("InvalidToken", "The reset token is invalid", nameof(request.Token))]);
        }

        if (resetToken.IsUsed || resetToken.DateExpired <= now)
        {
            resetToken.IsUsed = true;
            resetToken.DateUpdated = now;
            resetToken.UpdatedBy = SystemActor;
            unitOfWork.Update(resetToken);
            await unitOfWork.SaveChangesAsync(cancellationToken);

            return ApiResponse<ResetPasswordResponse>.Failed(
                StatusCodes.Status400BadRequest,
                "Reset token has expired or already been used",
                [new ApiError("ExpiredToken", "Request a new password reset link", nameof(request.Token))]);
        }

        var user = resetToken.User;
        var newHashedPassword = PasswordHasher.Hash(request.NewPassword);

        if (string.Equals(user.Password, newHashedPassword, StringComparison.Ordinal))
        {
            return ApiResponse<ResetPasswordResponse>.Failed(
                StatusCodes.Status409Conflict,
                "New password cannot be the same as old password",
                [new ApiError("PasswordReuse", "Use a different password", nameof(request.NewPassword))]);
        }

        user.Password = newHashedPassword;
        user.UpdatedBy = SystemActor;
        user.DateUpdated = now;

        resetToken.IsUsed = true;
        resetToken.DateUpdated = now;
        resetToken.UpdatedBy = SystemActor;

        unitOfWork.Update(user);
        unitOfWork.Update(resetToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        await emailProvider.SendAsync(
            EmailTemplates.PasswordReset(user.EmailAddress, user.FullName, appOptions.Value.AutVentLoginUrl, emailOptions),
            cancellationToken);

        var response = new ResetPasswordResponse
        {
            EmailAddress = user.EmailAddress,
            PasswordReset = true
        };

        return ApiResponse<ResetPasswordResponse>.Ok(response, "Password reset successful");
    }

    public async Task<ApiResponse<RefreshTokenResponse>> RefreshTokenAsync(RefreshTokenRequest request, CancellationToken cancellationToken = default)
    {
        var now = DateTime.UtcNow;

        var storedToken = await unitOfWork.Query<RefreshToken>()
            .Include(x => x.User)
            .FirstOrDefaultAsync(x => x.Token == request.RefreshToken, cancellationToken);

        if (storedToken is null)
        {
            return ApiResponse<RefreshTokenResponse>.Failed(
                StatusCodes.Status401Unauthorized,
                "Invalid refresh token",
                [new ApiError("InvalidRefreshToken", "The refresh token is invalid", nameof(request.RefreshToken))]);
        }

        if (storedToken.IsUsed || storedToken.IsRevoked || storedToken.DateExpired <= now)
        {
            return ApiResponse<RefreshTokenResponse>.Failed(
                StatusCodes.Status401Unauthorized,
                "Refresh token has expired or already been used",
                [new ApiError("ExpiredRefreshToken", "Please sign in again", nameof(request.RefreshToken))]);
        }

        storedToken.IsUsed = true;
        storedToken.IsActive = false;
        storedToken.DateUpdated = now;
        storedToken.UpdatedBy = SystemActor;
        unitOfWork.Update(storedToken);

        var (rawRefreshToken, refreshExpiresAt) = jwtTokenService.GenerateRefreshToken();

        var newRefreshToken = new RefreshToken
        {
            UserId = storedToken.UserId,
            Token = rawRefreshToken,
            DateExpired = refreshExpiresAt,
            IsUsed = false,
            IsRevoked = false,
            IsActive = true,
            CreatedBy = SystemActor,
            DateCreated = now
        };

        await unitOfWork.CreateAsync(newRefreshToken, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        var response = new RefreshTokenResponse
        {
            AccessToken = GenerateAccessToken(storedToken.User),
            RefreshToken = rawRefreshToken,
            RefreshTokenExpiresAtUtc = refreshExpiresAt
        };

        return ApiResponse<RefreshTokenResponse>.Ok(response, "Token refreshed successfully");
    }

    private string GenerateAccessToken(User user) => jwtTokenService.GenerateAccessToken(user);
}
