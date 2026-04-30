namespace EduNexis.Domain.Interfaces.Services;

public interface IEmailService
{
    /// <summary>
    /// Sends a single transactional email. Failures are logged but never thrown,
    /// so callers don't need to wrap calls in try/catch. The HTML body should
    /// already be wrapped in branded layout (use IEmailTemplateBuilder).
    /// </summary>
    Task SendAsync(string to, string subject, string body, CancellationToken ct = default);

    /// <summary>
    /// Sends the same email to multiple recipients (BCC-style).
    /// Useful for notifications that go to a class of students.
    /// </summary>
    Task SendAsync(IEnumerable<string> to, string subject, string body, CancellationToken ct = default);
}