namespace EduNexis.Domain.Interfaces.Services;

public interface IEmailTemplateBuilder
{
    /// <summary>
    /// Frontend base URL for building absolute links in emails.
    /// Configured via Frontend:BaseUrl setting.
    /// </summary>
    string FrontendBaseUrl { get; }

    /// <summary>
    /// Wraps content in EduNexis-branded HTML layout (header, footer, styling).
    /// </summary>
    string Build(string title, string bodyHtml);

    /// <summary>
    /// Builds an HTML CTA button.
    /// </summary>
    string Button(string url, string label);
}