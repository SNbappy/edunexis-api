namespace EduNexis.Domain.Interfaces.Services;

public interface IEmailTemplateBuilder
{
    /// <summary>
    /// Wraps content in EduNexis-branded HTML layout (header, footer, styling).
    /// </summary>
    /// <param name="title">Heading shown above content (e.g. "Verify your email")</param>
    /// <param name="bodyHtml">Inner HTML — paragraphs, buttons, etc.</param>
    string Build(string title, string bodyHtml);

    /// <summary>
    /// Builds an HTML CTA button.
    /// </summary>
    string Button(string url, string label);
}