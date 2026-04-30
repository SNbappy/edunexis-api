using EduNexis.Domain.Interfaces.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace EduNexis.API.Controllers;

/// <summary>
/// Development/diagnostic endpoints. Remove or guard with role check before production.
/// </summary>
[Authorize]
public class DevController : BaseController
{
    private readonly IEmailService _emailService;
    private readonly IEmailTemplateBuilder _templateBuilder;
    private readonly ILogger<DevController> _logger;

    public DevController(
        IEmailService emailService,
        IEmailTemplateBuilder templateBuilder,
        ILogger<DevController> logger)
    {
        _emailService = emailService;
        _templateBuilder = templateBuilder;
        _logger = logger;
    }

    /// <summary>
    /// Sends a test email to verify SMTP configuration end-to-end.
    /// Usage: POST /api/dev/test-email?to=you@example.com
    /// </summary>
    [HttpPost("test-email")]
    public async Task<IActionResult> TestEmail(
        [FromQuery] string to, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(to))
            return BadRequest(new { success = false, message = "Query parameter 'to' is required." });

        var bodyHtml =
            "<p>Hello,</p>" +
            "<p>This is a test email from EduNexis. If you can read this, SMTP is correctly configured.</p>" +
            "<p>Sent at: <strong>" + DateTime.UtcNow.ToString("u") + "</strong></p>" +
            "<p>You can safely ignore this message.</p>";

        var html = _templateBuilder.Build("Email service test", bodyHtml);

        await _emailService.SendAsync(to, "EduNexis email test", html, ct);

        _logger.LogInformation("Test email triggered to {To} by user {UserId}", to, CurrentUserId);

        return Ok(new
        {
            success = true,
            message = "Email send attempted. Check the inbox of " + to + " (and spam folder). Check server logs if not received.",
        });
    }
}