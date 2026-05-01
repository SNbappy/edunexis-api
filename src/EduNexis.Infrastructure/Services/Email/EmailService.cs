using EduNexis.Domain.Interfaces.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System.Net.Http.Json;
using System.Text.Json.Serialization;

namespace EduNexis.Infrastructure.Services.Email;

/// <summary>
/// Brevo (Sendinblue) HTTP API email sender.
/// Uses HTTPS POST to https://api.brevo.com/v3/smtp/email
/// because Render's free tier blocks outbound SMTP ports.
/// </summary>
public class EmailService : IEmailService
{
    private readonly HttpClient _http;
    private readonly ILogger<EmailService> _logger;
    private readonly bool _enabled;
    private readonly string _apiKey;
    private readonly string _fromEmail;
    private readonly string _fromName;

    private const string BrevoEndpoint = "https://api.brevo.com/v3/smtp/email";

    public EmailService(
        IHttpClientFactory httpFactory,
        ILogger<EmailService> logger,
        IConfiguration configuration)
    {
        _http = httpFactory.CreateClient("brevo-email");
        _http.Timeout = TimeSpan.FromSeconds(15);

        _logger = logger;
        _enabled = configuration.GetValue<bool>("Email:Enabled", true);
        _apiKey = configuration["Email:ApiKey"] ?? "";
        _fromEmail = configuration["Email:From"] ?? "noreply@edunexis.com";
        _fromName = configuration["Email:SenderName"] ?? "EduNexis";
    }

    public async Task SendAsync(
        string to, string subject, string body,
        CancellationToken ct = default)
    {
        if (!_enabled)
        {
            _logger.LogInformation("Email disabled by config. Skipping send to {Email}.", to);
            return;
        }

        if (string.IsNullOrWhiteSpace(to))
        {
            _logger.LogWarning("SendAsync called with empty recipient. Skipping.");
            return;
        }

        if (string.IsNullOrWhiteSpace(_apiKey))
        {
            _logger.LogError("Brevo API key not configured. Set Email:ApiKey environment variable.");
            return;
        }

        var payload = new BrevoEmailPayload
        {
            Sender = new BrevoAddress { Email = _fromEmail, Name = _fromName },
            To = new[] { new BrevoAddress { Email = to } },
            Subject = subject,
            HtmlContent = body,
        };

        await SendInternalAsync(payload, to, ct);
    }

    public async Task SendAsync(
        IEnumerable<string> to, string subject, string body,
        CancellationToken ct = default)
    {
        if (!_enabled)
        {
            _logger.LogInformation("Email disabled by config. Skipping batch send.");
            return;
        }

        var recipients = to
            .Where(r => !string.IsNullOrWhiteSpace(r))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();

        if (recipients.Count == 0)
        {
            _logger.LogWarning("Batch SendAsync called with no valid recipients. Skipping.");
            return;
        }

        if (string.IsNullOrWhiteSpace(_apiKey))
        {
            _logger.LogError("Brevo API key not configured. Set Email:ApiKey environment variable.");
            return;
        }

        // Brevo accepts up to 99 recipients per call, but each appears in the To header
        // for everyone (privacy leak). For notifications we send one call per recipient.
        foreach (var recipient in recipients)
        {
            var payload = new BrevoEmailPayload
            {
                Sender = new BrevoAddress { Email = _fromEmail, Name = _fromName },
                To = new[] { new BrevoAddress { Email = recipient } },
                Subject = subject,
                HtmlContent = body,
            };
            await SendInternalAsync(payload, recipient, ct);
        }

        _logger.LogInformation("Batch email completed: {Count} recipients, subject {Subject}",
            recipients.Count, subject);
    }

    private async Task SendInternalAsync(
        BrevoEmailPayload payload, string recipient, CancellationToken ct)
    {
        try
        {
            using var request = new HttpRequestMessage(HttpMethod.Post, BrevoEndpoint);
            request.Headers.Add("api-key", _apiKey);
            request.Headers.Add("accept", "application/json");
            request.Content = JsonContent.Create(payload);

            using var response = await _http.SendAsync(request, ct);

            if (response.IsSuccessStatusCode)
            {
                _logger.LogInformation("Email sent via Brevo to {Email}: {Subject}",
                    recipient, payload.Subject);
            }
            else
            {
                var error = await response.Content.ReadAsStringAsync(ct);
                _logger.LogWarning(
                    "Brevo rejected email to {Email}: HTTP {Status} {Error}",
                    recipient, (int)response.StatusCode, error);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send email to {Email} via Brevo HTTP API", recipient);
        }
    }
}

internal sealed class BrevoEmailPayload
{
    [JsonPropertyName("sender")]
    public BrevoAddress Sender { get; set; } = new();

    [JsonPropertyName("to")]
    public BrevoAddress[] To { get; set; } = Array.Empty<BrevoAddress>();

    [JsonPropertyName("subject")]
    public string Subject { get; set; } = "";

    [JsonPropertyName("htmlContent")]
    public string HtmlContent { get; set; } = "";
}

internal sealed class BrevoAddress
{
    [JsonPropertyName("email")]
    public string Email { get; set; } = "";

    [JsonPropertyName("name")]
    public string? Name { get; set; }
}