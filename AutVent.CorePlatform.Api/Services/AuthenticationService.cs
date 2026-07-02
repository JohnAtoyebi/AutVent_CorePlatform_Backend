using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using AutVent.CorePlatform.Api.Common.Email;
using AutVent.CorePlatform.Api.Common.Requests;
using AutVent.CorePlatform.Api.Common.Responses;
using AutVent.CorePlatform.Api.Common.Security;
using AutVent.CorePlatform.Api.Infrastructure.Email;
using AutVent.CorePlatform.Domain.Entities;
using AutVent.CorePlatform.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

namespace AutVent.CorePlatform.Api.Services;

public sealed class AuthenticationService(
    IUnitOfWork unitOfWork,
    IConfiguration configuration,
    IEmailProvider emailProvider,
    IOptions<EmailOptions> emailOptions) : IAuthenticationService
{
    private const string SystemActor = "system";

    public async Task<ApiResponse<SignInResponse>> SignInAsync(SignInRequest request, CancellationToken cancellationToken = default)
    {
        var normalizedEmail = request.EmailAddress.Trim().ToLowerInvariant();
        var hashedPassword = PasswordHasher.Hash(request.Password);

        var user = await unitOfWork.Query<User>()
            .FirstOrDefaultAsync(x => x.EmailAddress.ToLower() == normalizedEmail, cancellationToken);

        if (user is null)
        {
            return ApiResponse<SignInResponse>.Failed(
                StatusCodes.Status404NotFound,
                "User not found",
                [new ApiError("UserNotFound", "No user found for this email", nameof(request.EmailAddress))]);
        }

        if (!user.IsActive)
        {
            return ApiResponse<SignInResponse>.Failed(
                StatusCodes.Status403Forbidden,
                "Email is not verified",
                [new ApiError("EmailNotVerified", "Verify your email before signing in", nameof(request.EmailAddress))]);
        }

        if (!string.Equals(user.Password, hashedPassword, StringComparison.Ordinal))
        {
            return ApiResponse<SignInResponse>.Failed(
                StatusCodes.Status401Unauthorized,
                "Invalid credentials",
                [new ApiError("InvalidCredentials", "Email or password is incorrect")]);
        }

        var response = new SignInResponse
        {
            UserId = user.Id,
            FullName = user.FullName,
            EmailAddress = user.EmailAddress,
            AccessToken = GenerateAccessToken(user)
        };

        return ApiResponse<SignInResponse>.Ok(response, "Sign in successful");
    }

    public async Task<ApiResponse<ResetPasswordResponse>> ResetPasswordAsync(ResetPasswordRequest request, CancellationToken cancellationToken = default)
    {
        var normalizedEmail = request.EmailAddress.Trim().ToLowerInvariant();

        var user = await unitOfWork.Query<User>()
            .FirstOrDefaultAsync(x => x.EmailAddress.ToLower() == normalizedEmail, cancellationToken);

        if (user is null)
        {
            return ApiResponse<ResetPasswordResponse>.Failed(
                StatusCodes.Status404NotFound,
                "User not found",
                [new ApiError("UserNotFound", "No user found for this email", nameof(request.EmailAddress))]);
        }

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
        user.DateUpdated = DateTime.UtcNow;

        unitOfWork.Update(user);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        await emailProvider.SendAsync(
            EmailTemplates.PasswordReset(normalizedEmail, user.FullName, emailOptions),
            cancellationToken);

        var response = new ResetPasswordResponse
        {
            EmailAddress = user.EmailAddress,
            PasswordReset = true
        };

        return ApiResponse<ResetPasswordResponse>.Ok(response, "Password reset successful");
    }

    private string GenerateAccessToken(User user)
    {
        var jwt = configuration.GetSection("Jwt").Get<JwtOptions>()
            ?? throw new InvalidOperationException("JWT settings are missing.");

        if (string.IsNullOrWhiteSpace(jwt.Key) || string.IsNullOrWhiteSpace(jwt.Issuer) || string.IsNullOrWhiteSpace(jwt.Audience))
        {
            throw new InvalidOperationException("JWT settings are invalid.");
        }

        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwt.Key));
        var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var claims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Sub, user.Id.ToString()),
            new(JwtRegisteredClaimNames.Email, user.EmailAddress),
            new(JwtRegisteredClaimNames.Name, user.FullName),
            new(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
        };

        var token = new JwtSecurityToken(
            issuer: jwt.Issuer,
            audience: jwt.Audience,
            claims: claims,
            expires: DateTime.UtcNow.AddMinutes(jwt.ExpiryMinutes <= 0 ? 60 : jwt.ExpiryMinutes),
            signingCredentials: credentials);

        var tokenString = new JwtSecurityTokenHandler().WriteToken(token);
        return tokenString;
    }
}
