using AutVent.CorePlatform.Api.Common.Email;
using Microsoft.Extensions.Options;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;

namespace AutVent.CorePlatform.Api.Infrastructure.Email;

public sealed class ResendEmailProvider(
    IOptions<EmailOptions> options,
    IHttpClientFactory httpClientFactory,
    ILogger<ResendEmailProvider> logger) : IEmailProvider
{
    private readonly EmailOptions _options = options.Value;

    public async Task SendAsync(EmailMessage message, CancellationToken cancellationToken = default)
    {
        try
        {
            var resend = _options.Resend;

            if (string.IsNullOrWhiteSpace(resend.ApiKey))
            {
                logger.LogWarning("Resend API key is not configured. Email to {To} was not sent.", message.To);
                return;
            }

            if (string.IsNullOrWhiteSpace(message.TemplateAlias))
            {
                logger.LogWarning("No template alias provided. Email to {To} was not sent.", message.To);
                return;
            }

            var client = httpClientFactory.CreateClient(nameof(ResendEmailProvider));
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", resend.ApiKey);

            var attachments = message.Attachments?.Select(a => new
            {
                filename = a.Filename,
                content = a.Content,
                content_type = a.ContentType
            }).ToList();

            var payload = new
            {
                from = $"{_options.FromName} <{_options.FromAddress}>",
                to = new[] { message.To },
                template = new
                {
                    id = message.TemplateAlias,
                    variables = message.TemplateVariables ?? new Dictionary<string, object>()
                },
                attachments = attachments
            };

            var json = JsonSerializer.Serialize(payload, new JsonSerializerOptions
            {
                DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull
            });

            var content = new StringContent(json, Encoding.UTF8, "application/json");
            var httpResponse = await client.PostAsync($"{resend.BaseUrl}/emails", content, cancellationToken);

            if (!httpResponse.IsSuccessStatusCode)
            {
                var responseBody = await httpResponse.Content.ReadAsStringAsync(cancellationToken);
                logger.LogError(
                    "Resend returned {StatusCode} for email to {To}. Response: {Body}",
                    (int)httpResponse.StatusCode, message.To, responseBody);
            }
            else
            {
                logger.LogInformation("Email sent successfully to {To} using template '{Template}'.", message.To, message.TemplateAlias);
            }
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Unexpected error sending email to {To}.", message.To);
        }
    }
}
