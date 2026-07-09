using AutVent.CorePlatform.Api.Common.Requests;
using AutVent.CorePlatform.Api.Common.Responses;
using AutVent.CorePlatform.Api.Common.Security;
using AutVent.CorePlatform.Api.Common.Email;
using AutVent.CorePlatform.Api.Infrastructure.Email;
using AutVent.CorePlatform.Domain.Entities;
using AutVent.CorePlatform.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace AutVent.CorePlatform.Api.Services;

public sealed class OnboardingService(IUnitOfWork unitOfWork, IEmailProvider emailProvider, IOptions<EmailOptions> emailOptions) : IOnboardingService
{
    private const string SystemActor = "system";

    public async Task<ApiResponse<RegisterUserResponse>> RegisterAsync(RegisterUserRequest request, CancellationToken cancellationToken = default)
    {
        var normalizedEmail = request.EmailAddress.Trim().ToLowerInvariant();
        var normalizedPhone = request.PhoneNumber.Trim();

        var emailExists = await unitOfWork.Query<User>()
            .AnyAsync(x => x.EmailAddress.ToLower() == normalizedEmail, cancellationToken);

        if (emailExists)
        {
            return ApiResponse<RegisterUserResponse>.Failed(
                StatusCodes.Status409Conflict,
                "A user with this email already exists",
                [new ApiError("DuplicateEmail", "Email address already exists", nameof(request.EmailAddress))]);
        }

        var phoneExists = await unitOfWork.Query<User>()
            .AnyAsync(x => x.PhoneNumber == normalizedPhone, cancellationToken);

        if (phoneExists)
        {
            return ApiResponse<RegisterUserResponse>.Failed(
                StatusCodes.Status409Conflict,
                "A user with this phone number already exists",
                [new ApiError("DuplicatePhone", "Phone number already exists", nameof(request.PhoneNumber))]);
        }

        if (!string.IsNullOrWhiteSpace(request.ReferralCode))
        {
            var referralCodeExists = await unitOfWork.Query<User>()
                .AnyAsync(x => x.ReferralCode == request.ReferralCode.Trim(), cancellationToken);

            if (!referralCodeExists)
            {
                return ApiResponse<RegisterUserResponse>.Failed(
                    StatusCodes.Status400BadRequest,
                    "Invalid referral code",
                    [new ApiError("InvalidReferralCode", "The referral code provided is not valid", nameof(request.ReferralCode))]);
            }
        }

        var utcNow = DateTime.UtcNow;
        var referralCode = await GenerateUniqueReferralCodeAsync(cancellationToken);

        User? referrer = null;
        if (!string.IsNullOrWhiteSpace(request.ReferralCode))
        {
            referrer = await unitOfWork.Query<User>()
                .FirstOrDefaultAsync(x => x.ReferralCode == request.ReferralCode.Trim(), cancellationToken);
        }

        var user = new User
        {
            FullName = request.FullName.Trim(),
            EmailAddress = normalizedEmail,
            PhoneNumber = normalizedPhone,
            Password = PasswordHasher.Hash(request.Password),
            ReferralCode = referralCode,
            IsActive = false,
            CreatedBy = SystemActor,
            DateCreated = utcNow
        };

        await unitOfWork.CreateAsync(user, cancellationToken);

        if (referrer is not null)
        {
            var referralRecord = new ReferralRecord
            {
                ReferrerId = referrer.Id,
                ReferredUser = user,
                ReferralCode = referrer.ReferralCode!,
                IsActive = true,
                CreatedBy = SystemActor,
                DateCreated = utcNow
            };
            await unitOfWork.CreateAsync(referralRecord, cancellationToken);
        }

        var otp = await CreateAndTrackOtpAsync(normalizedEmail, cancellationToken);

        await unitOfWork.SaveChangesAsync(cancellationToken);

        await emailProvider.SendAsync(
            EmailTemplates.OtpVerification(normalizedEmail, user.FullName, otp.Code, otp.DateExpired, emailOptions),
            cancellationToken);

        var response = new RegisterUserResponse
        {
            UserId = user.Id,
            FullName = user.FullName,
            EmailAddress = user.EmailAddress,
            PhoneNumber = user.PhoneNumber,
            ReferralCode = user.ReferralCode,
            IsActive = user.IsActive,
            OtpExpiresAtUtc = otp.DateExpired
        };

        return ApiResponse<RegisterUserResponse>.Created(response, "User registered successfully. Please verify your email with the OTP.");
    }

    public async Task<ApiResponse<VerifyOtpResponse>> VerifyOtpAsync(VerifyOtpRequest request, CancellationToken cancellationToken = default)
    {
        var normalizedEmail = request.EmailAddress.Trim().ToLowerInvariant();
        var now = DateTime.UtcNow;

        var user = await unitOfWork.Query<User>()
            .FirstOrDefaultAsync(x => x.EmailAddress.ToLower() == normalizedEmail, cancellationToken);

        if (user is null)
        {
            return ApiResponse<VerifyOtpResponse>.Failed(
                StatusCodes.Status404NotFound,
                "User not found",
                [new ApiError("UserNotFound", "No user found for this email", nameof(request.EmailAddress))]);
        }

        var otp = await unitOfWork.Query<Otp>()
            .Where(x => x.EmailAddress.ToLower() == normalizedEmail && x.Code == request.Code)
            .OrderByDescending(x => x.Id)
            .FirstOrDefaultAsync(cancellationToken);

        if (otp is null)
        {
            return ApiResponse<VerifyOtpResponse>.Failed(
                StatusCodes.Status400BadRequest,
                "Invalid OTP",
                [new ApiError("InvalidOtp", "OTP is invalid", nameof(request.Code))]);
        }

        if (otp.IsExpired || otp.DateExpired <= now)
        {
            otp.IsExpired = true;
            otp.DateUpdated = now;
            otp.UpdatedBy = SystemActor;
            unitOfWork.Update(otp);
            await unitOfWork.SaveChangesAsync(cancellationToken);

            return ApiResponse<VerifyOtpResponse>.Failed(
                StatusCodes.Status400BadRequest,
                "OTP has expired",
                [new ApiError("ExpiredOtp", "OTP has expired. Request a new one", nameof(request.Code))]);
        }

        user.IsActive = true;
        user.DateUpdated = now;
        user.UpdatedBy = SystemActor;

        otp.IsExpired = true;
        otp.DateUpdated = now;
        otp.UpdatedBy = SystemActor;

        unitOfWork.Update(user);
        unitOfWork.Update(otp);

        await unitOfWork.SaveChangesAsync(cancellationToken);

        var response = new VerifyOtpResponse
        {
            EmailAddress = normalizedEmail,
            IsVerified = true
        };

        return ApiResponse<VerifyOtpResponse>.Ok(response, "Email verified successfully");
    }

    public async Task<ApiResponse<ResendOtpResponse>> ResendOtpAsync(ResendOtpRequest request, CancellationToken cancellationToken = default)
    {
        var normalizedEmail = request.EmailAddress.Trim().ToLowerInvariant();

        var user = await unitOfWork.Query<User>()
            .FirstOrDefaultAsync(x => x.EmailAddress.ToLower() == normalizedEmail, cancellationToken);

        if (user is null)
        {
            return ApiResponse<ResendOtpResponse>.Failed(
                StatusCodes.Status404NotFound,
                "User not found",
                [new ApiError("UserNotFound", "No user found for this email", nameof(request.EmailAddress))]);
        }

        if (user.IsActive)
        {
            return ApiResponse<ResendOtpResponse>.Failed(
                StatusCodes.Status409Conflict,
                "User already verified",
                [new ApiError("AlreadyVerified", "Email is already verified", nameof(request.EmailAddress))]);
        }

        var otp = await CreateAndTrackOtpAsync(normalizedEmail, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        await emailProvider.SendAsync(
            EmailTemplates.OtpVerification(normalizedEmail, user.FullName, otp.Code, otp.DateExpired, emailOptions),
            cancellationToken);

        var response = new ResendOtpResponse
        {
            EmailAddress = normalizedEmail,
            ExpiresAtUtc = otp.DateExpired
        };

        return ApiResponse<ResendOtpResponse>.Ok(response, "OTP resent successfully");
    }

    private async Task<Otp> CreateAndTrackOtpAsync(string normalizedEmail, CancellationToken cancellationToken)
    {
        var now = DateTime.UtcNow;

        var activeOtps = await unitOfWork.Query<Otp>()
            .Where(x => x.EmailAddress.ToLower() == normalizedEmail && !x.IsExpired)
            .ToListAsync(cancellationToken);

        foreach (var activeOtp in activeOtps)
        {
            activeOtp.IsExpired = true;
            activeOtp.DateUpdated = now;
            activeOtp.UpdatedBy = SystemActor;
            unitOfWork.Update(activeOtp);
        }

        var otp = new Otp
        {
            Code = GenerateOtpCode(),
            EmailAddress = normalizedEmail,
            IsExpired = false,
            DateExpired = now.AddMinutes(10),
            IsActive = true,
            CreatedBy = SystemActor,
            DateCreated = now
        };

        await unitOfWork.CreateAsync(otp, cancellationToken);
        return otp;
    }

    private static string GenerateOtpCode() => Random.Shared.Next(100000, 1000000).ToString();

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
