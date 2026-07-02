namespace AutVent.CorePlatform.Api.Common.Email;

public interface IEmailProvider
{
    Task SendAsync(EmailMessage message, CancellationToken cancellationToken = default);
}
