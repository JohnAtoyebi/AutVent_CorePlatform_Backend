using AutVent.CorePlatform.Api.Common.Email;
using Microsoft.Extensions.Options;

namespace AutVent.CorePlatform.Api.Infrastructure.Email;

public static class EmailTemplates
{
    public static EmailMessage OtpVerification(
        string toEmail,
        string fullName,
        string otpCode,
        DateTime expiresAtUtc,
        IOptions<EmailOptions> options)
    {
        var templateAlias = options.Value.Resend.Templates.OtpVerification;

        if (string.IsNullOrWhiteSpace(templateAlias))
        {
            throw new InvalidOperationException("OtpVerification template alias is not configured.");
        }

        return new EmailMessage
        {
            To = toEmail,
            Subject = "Verify your email",
            TemplateAlias = templateAlias,
            TemplateVariables = new Dictionary<string, object>
            {
                ["fullName"] = fullName,
                ["otp"] = otpCode,
                ["expiryMinutes"] = 10,
                ["year"] = expiresAtUtc.Year
            }
        };
    }

    public static EmailMessage PasswordReset(
        string toEmail,
        string fullName,
        IOptions<EmailOptions> options)
    {
        var templateAlias = options.Value.Resend.Templates.PasswordReset;

        if (string.IsNullOrWhiteSpace(templateAlias))
        {
            throw new InvalidOperationException("PasswordReset template alias is not configured.");
        }

        return new EmailMessage
        {
            To = toEmail,
            Subject = "Password Reset Successful",
            TemplateAlias = templateAlias,
            TemplateVariables = new Dictionary<string, object>
            {
                ["fullName"] = fullName,
                ["year"] = DateTime.UtcNow.Year
            }
        };
    }
}
