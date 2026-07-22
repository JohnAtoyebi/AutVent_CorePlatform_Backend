using AutVent.CorePlatform.Api.Common.Requests;
using AutVent.CorePlatform.Api.Common.Responses;
using AutVent.CorePlatform.Api.Common.Security;
using AutVent.CorePlatform.Domain.Entities;
using AutVent.CorePlatform.Domain.Enums;
using AutVent.CorePlatform.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace AutVent.CorePlatform.Api.Services;

public sealed class UserService(IUnitOfWork unitOfWork, IAuditLogService auditLogService) : IUserService
{
    private const string SystemActor = "system";

    public async Task<ApiResponse<UserProfileResponse>> GetCurrentUserAsync(long userId, CancellationToken cancellationToken = default)
    {
        var user = await unitOfWork.Query<User>()
            .FirstOrDefaultAsync(x => x.Id == userId, cancellationToken);

        if (user is null)
        {
            return ApiResponse<UserProfileResponse>.Failed(
                StatusCodes.Status404NotFound,
                "User not found",
                [new ApiError("UserNotFound", "No user found for this id", "userId")]);
        }

        // Backfill: accounts created before referral codes were introduced arrive with null.
        // Generate and persist a code now so the caller always gets a non-null value.
        if (string.IsNullOrWhiteSpace(user.ReferralCode))
        {
            user.ReferralCode = await GenerateUniqueReferralCodeAsync(cancellationToken);
            user.UpdatedBy = SystemActor;
            user.DateUpdated = DateTime.UtcNow;
            unitOfWork.Update(user);
            await unitOfWork.SaveChangesAsync(cancellationToken);
        }

        var response = new UserProfileResponse
        {
            Id = user.Id,
            FullName = user.FullName,
            EmailAddress = user.EmailAddress,
            PhoneNumber = user.PhoneNumber,
            ReferralCode = user.ReferralCode,
            IsActive = user.IsActive,
            ProfilePhotoUrl = user.ProfilePhotoUrl,
            MemberSince = user.DateCreated
        };

        return ApiResponse<UserProfileResponse>.Ok(response);
    }

    public async Task<ApiResponse<UpdateProfileResponse>> UpdateProfileAsync(long userId, UpdateProfileRequest request, CancellationToken cancellationToken = default)
    {
        var now = DateTime.UtcNow;

        var user = await unitOfWork.Query<User>()
            .FirstOrDefaultAsync(x => x.Id == userId, cancellationToken);

        if (user is null)
        {
            return ApiResponse<UpdateProfileResponse>.Failed(
                StatusCodes.Status404NotFound,
                "User not found",
                [new ApiError("UserNotFound", "No user found for this id", "userId")]);
        }

        var normalizedPhone = request.PhoneNumber.Trim();

        if (normalizedPhone != user.PhoneNumber)
        {
            var phoneExists = await unitOfWork.Query<User>()
                .AnyAsync(x => x.PhoneNumber == normalizedPhone && x.Id != userId, cancellationToken);

            if (phoneExists)
            {
                return ApiResponse<UpdateProfileResponse>.Failed(
                    StatusCodes.Status409Conflict,
                    "Phone number already in use",
                    [new ApiError("DuplicatePhone", "This phone number is already associated with another account", nameof(request.PhoneNumber))]);
            }
        }

        user.FullName = request.FullName.Trim();
        user.PhoneNumber = normalizedPhone;
        if (request.ProfilePhotoUrl is not null)
            user.ProfilePhotoUrl = request.ProfilePhotoUrl.Trim();
        user.UpdatedBy = SystemActor;
        user.DateUpdated = now;

        unitOfWork.Update(user);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return ApiResponse<UpdateProfileResponse>.Ok(
            new UpdateProfileResponse
            {
                Id = user.Id,
                FullName = user.FullName,
                PhoneNumber = user.PhoneNumber,
                ProfilePhotoUrl = user.ProfilePhotoUrl
            },
            "Profile updated successfully");
    }

    public async Task<ApiResponse<ChangePasswordResponse>> ChangePasswordAsync(long userId, ChangePasswordRequest request, CancellationToken cancellationToken = default)
    {
        var now = DateTime.UtcNow;

        var user = await unitOfWork.Query<User>()
            .FirstOrDefaultAsync(x => x.Id == userId, cancellationToken);

        if (user is null)
        {
            return ApiResponse<ChangePasswordResponse>.Failed(
                StatusCodes.Status404NotFound,
                "User not found",
                [new ApiError("UserNotFound", "No user found for this id", "userId")]);
        }

        if (!string.Equals(user.Password, PasswordHasher.Hash(request.CurrentPassword), StringComparison.Ordinal))
        {
            return ApiResponse<ChangePasswordResponse>.Failed(
                StatusCodes.Status401Unauthorized,
                "Current password is incorrect",
                [new ApiError("InvalidPassword", "The current password you entered is incorrect", nameof(request.CurrentPassword))]);
        }

        user.Password = PasswordHasher.Hash(request.NewPassword);
        user.UpdatedBy = SystemActor;
        user.DateUpdated = now;

        unitOfWork.Update(user);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        await auditLogService.LogAsync(
            userId,
            AuditAction.UserPasswordChanged,
            nameof(User),
            $"User '{user.EmailAddress}' changed their password.",
            entityId: userId,
            cancellationToken: cancellationToken);

        return ApiResponse<ChangePasswordResponse>.Ok(
            new ChangePasswordResponse { UserId = user.Id },
            "Password changed successfully");
    }

    public async Task<ApiResponse<ChangeEmailResponse>> ChangeEmailAsync(long userId, ChangeEmailRequest request, CancellationToken cancellationToken = default)
    {
        var now = DateTime.UtcNow;

        var user = await unitOfWork.Query<User>()
            .FirstOrDefaultAsync(x => x.Id == userId, cancellationToken);

        if (user is null)
        {
            return ApiResponse<ChangeEmailResponse>.Failed(
                StatusCodes.Status404NotFound,
                "User not found",
                [new ApiError("UserNotFound", "No user found for this id", "userId")]);
        }

        if (!string.Equals(user.Password, PasswordHasher.Hash(request.CurrentPassword), StringComparison.Ordinal))
        {
            return ApiResponse<ChangeEmailResponse>.Failed(
                StatusCodes.Status401Unauthorized,
                "Current password is incorrect",
                [new ApiError("InvalidPassword", "The current password you entered is incorrect", nameof(request.CurrentPassword))]);
        }

        var normalizedEmail = request.NewEmailAddress.Trim().ToLowerInvariant();

        if (string.Equals(user.EmailAddress, normalizedEmail, StringComparison.Ordinal))
        {
            return ApiResponse<ChangeEmailResponse>.Failed(
                StatusCodes.Status409Conflict,
                "New email is the same as the current email",
                [new ApiError("SameEmail", "The new email address must be different from the current one", nameof(request.NewEmailAddress))]);
        }

        var emailExists = await unitOfWork.Query<User>()
            .AnyAsync(x => x.EmailAddress.ToLower() == normalizedEmail && x.Id != userId, cancellationToken);

        if (emailExists)
        {
            return ApiResponse<ChangeEmailResponse>.Failed(
                StatusCodes.Status409Conflict,
                "Email address already in use",
                [new ApiError("DuplicateEmail", "This email address is already associated with another account", nameof(request.NewEmailAddress))]);
        }

        user.EmailAddress = normalizedEmail;
        user.IsEmailVerified = false;
        user.IsActive = false;
        user.UpdatedBy = SystemActor;
        user.DateUpdated = now;

        unitOfWork.Update(user);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return ApiResponse<ChangeEmailResponse>.Ok(
            new ChangeEmailResponse { UserId = user.Id, NewEmailAddress = normalizedEmail },
            "Email address updated. Please verify your new email before signing in again");
    }

    private async Task<string> GenerateUniqueReferralCodeAsync(CancellationToken cancellationToken)
    {
        string code;
        do
        {
            code = ReferralCodeGenerator.Generate();
        }
        while (await unitOfWork.Query<User>().AnyAsync(x => x.ReferralCode == code, cancellationToken));

        return code;
    }
}

