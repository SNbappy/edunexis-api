using EduNexis.Domain.Interfaces.Services;
using FluentEmail.Core;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace EduNexis.Infrastructure.Services.Email;

public class EmailService : IEmailService
{
    private readonly IFluentEmail _fluentEmail;
    private readonly ILogger<EmailService> _logger;
    private readonly bool _enabled;

    public EmailService(
        IFluentEmail fluentEmail,
        ILogger<EmailService> logger,
        IConfiguration configuration)
    {
        _fluentEmail = fluentEmail;
        _logger = logger;
        _enabled = configuration.GetValue<bool>("Email:Enabled", true);
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

        try
        {
            var result = await _fluentEmail
                .To(to)
                .Subject(subject)
                .Body(body, isHtml: true)
                .SendAsync(ct);

            if (!result.Successful)
            {
                _logger.LogWarning(
                    "Email to {Email} reported not successful: {Errors}",
                    to, string.Join("; ", result.ErrorMessages));
            }
            else
            {
                _logger.LogInformation("Email sent to {Email}: {Subject}", to, subject);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send email to {Email}", to);
        }
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

        // Send each individually so one failure doesn't poison the batch.
        // Could be parallelized later; for now sequential keeps Gmail SMTP happy
        // (Gmail rate-limits concurrent connections from a single account).
        foreach (var recipient in recipients)
        {
            try
            {
                await _fluentEmail
                    .To(recipient)
                    .Subject(subject)
                    .Body(body, isHtml: true)
                    .SendAsync(ct);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to send batch email to {Email}", recipient);
            }
        }

        _logger.LogInformation("Batch email completed: {Count} recipients, subject {Subject}",
            recipients.Count, subject);
    }
}