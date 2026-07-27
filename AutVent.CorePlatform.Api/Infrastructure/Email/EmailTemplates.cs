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

        var expiryMinutes = (int)Math.Ceiling((expiresAtUtc - DateTime.UtcNow).TotalMinutes);

        return new EmailMessage
        {
            To = toEmail,
            Subject = "Verify your email",
            TemplateAlias = templateAlias,
            ExpiresAtUtc = expiresAtUtc,
            TemplateVariables = new Dictionary<string, object>
            {
                ["fullName"] = fullName.Split(' ')[0],
                ["otp"] = otpCode,
                ["expiryMinutes"] = expiryMinutes,
                ["year"] = expiresAtUtc.Year
            }
        };
    }

    public static EmailMessage PasswordReset(
        string toEmail,
        string fullName,
        string loginLink,
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
                ["fullName"] = fullName.Split(' ')[0],
                ["loginLink"] = loginLink,
                ["year"] = DateTime.UtcNow.Year
            }
        };
    }

    public static EmailMessage BusinessWelcome(
        string toEmail,
        string fullName,
        string businessName,
        IOptions<EmailOptions> options)
    {
        var templateAlias = options.Value.Resend.Templates.BusinessWelcome;

        if (string.IsNullOrWhiteSpace(templateAlias))
        {
            throw new InvalidOperationException("BusinessWelcome template alias is not configured.");
        }

        return new EmailMessage
        {
            To = toEmail,
            Subject = "Welcome to AutVent!",
            TemplateAlias = templateAlias,
            TemplateVariables = new Dictionary<string, object>
            {
                ["fullName"] = fullName.Split(' ')[0],
                ["businessName"] = businessName,
                ["year"] = DateTime.UtcNow.Year
            }
        };
    }

    public static EmailMessage ForgotPassword(
        string toEmail,
        string fullName,
        string resetLink,
        DateTime expiresAtUtc,
        IOptions<EmailOptions> options)
    {
        var templateAlias = options.Value.Resend.Templates.ForgotPassword;

        if (string.IsNullOrWhiteSpace(templateAlias))
        {
            throw new InvalidOperationException("ForgotPassword template alias is not configured.");
        }

        return new EmailMessage
        {
            To = toEmail,
            Subject = "Reset your password",
            TemplateAlias = templateAlias,
            ExpiresAtUtc = expiresAtUtc,
            TemplateVariables = new Dictionary<string, object>
            {
                ["fullName"] = fullName.Split(' ')[0],
                ["resetLink"] = resetLink,
                ["year"] = expiresAtUtc.Year
            }
        };
    }

    public static EmailMessage ContactSupport(
        string supportEmail,
        string fullName,
        string fromEmail,
        string message,
        IOptions<EmailOptions> options)
    {
        var templateAlias = options.Value.Resend.Templates.ContactSupport;

        if (string.IsNullOrWhiteSpace(templateAlias))
        {
            throw new InvalidOperationException("ContactSupport template alias is not configured.");
        }

        return new EmailMessage
        {
            To = supportEmail,
            Subject = $"Support Request from {fullName}",
            TemplateAlias = templateAlias,
            TemplateVariables = new Dictionary<string, object>
            {
                ["fullName"] = fullName,
                ["fromEmail"] = fromEmail,
                ["message"] = message,
                ["year"] = DateTime.UtcNow.Year
            }
        };
    }

    public static EmailMessage WaitlistConfirmation(
        string toEmail,
        string fullName,
        string websiteUrl,
        IOptions<EmailOptions> options)
    {
        var templateAlias = options.Value.Resend.Templates.WaitlistConfirmation;

        if (string.IsNullOrWhiteSpace(templateAlias))
        {
            throw new InvalidOperationException("WaitlistConfirmation template alias is not configured.");
        }

        return new EmailMessage
        {
            To = toEmail,
            Subject = "You're on the waitlist!",
            TemplateAlias = templateAlias,
            TemplateVariables = new Dictionary<string, object>
            {
                ["fullName"] = fullName.Split(' ')[0],
                ["websiteUrl"] = websiteUrl,
                ["year"] = DateTime.UtcNow.Year
            }
        };
    }

    public static EmailMessage InvoiceNotification(
        string toEmail,
        string customerName,
        string invoiceNumber,
        decimal totalAmount,
        DateTime dueDate,
        List<EmailAttachment>? attachments = null,
        IOptions<EmailOptions>? options = null)
    {
        var templateAlias = options?.Value.Resend.Templates.InvoiceNotification;

        if (string.IsNullOrWhiteSpace(templateAlias))
        {
            throw new InvalidOperationException("InvoiceNotification template alias is not configured.");
        }

        return new EmailMessage
        {
            To = toEmail,
            Subject = $"Invoice {invoiceNumber} - Payment",
            TemplateAlias = templateAlias,
            TemplateVariables = new Dictionary<string, object>
            {
                ["customerName"] = customerName,
                ["invoiceNumber"] = invoiceNumber,
                ["totalAmount"] = totalAmount,
                ["dueDate"] = dueDate.ToString("yyyy-MM-dd"),
                ["year"] = DateTime.UtcNow.Year
            },
            Attachments = attachments
        };
    }
}
