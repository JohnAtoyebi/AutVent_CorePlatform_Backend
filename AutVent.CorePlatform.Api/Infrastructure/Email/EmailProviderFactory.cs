using AutVent.CorePlatform.Api.Common.Email;
using Microsoft.Extensions.Options;

namespace AutVent.CorePlatform.Api.Infrastructure.Email;

public sealed class EmailProviderFactory(IServiceProvider serviceProvider, IOptions<EmailOptions> options) : IEmailProvider
{
    private readonly EmailOptions _options = options.Value;

    public Task SendAsync(EmailMessage message, CancellationToken cancellationToken = default)
    {
        var provider = ResolveProvider();
        return provider.SendAsync(message, cancellationToken);
    }

    private IEmailProvider ResolveProvider() =>
        _options.Provider?.ToLowerInvariant() switch
        {
            "resend" => (IEmailProvider)serviceProvider.GetRequiredService<ResendEmailProvider>(),
            _ => throw new InvalidOperationException($"Email provider '{_options.Provider}' is not supported.")
        };
}
