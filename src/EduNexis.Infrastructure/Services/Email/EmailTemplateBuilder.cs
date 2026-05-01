using EduNexis.Domain.Interfaces.Services;
using Microsoft.Extensions.Configuration;
using System.Text;

namespace EduNexis.Infrastructure.Services.Email;

public class EmailTemplateBuilder : IEmailTemplateBuilder
{
    public string FrontendBaseUrl { get; }

    public EmailTemplateBuilder(IConfiguration configuration)
    {
        FrontendBaseUrl = (configuration["Frontend:BaseUrl"] ?? "http://localhost:5173")
            .TrimEnd('/');
    }

    private const string TealColor = "#0d9488"; // Tailwind teal-600
    private const string TealDark = "#0f766e";  // teal-700
    private const string TextDark = "#1c1917";  // stone-900
    private const string TextMuted = "#78716c"; // stone-500
    private const string Bg = "#fafaf9";        // stone-50
    private const string Card = "#ffffff";
    private const string Border = "#e7e5e4";    // stone-200

    public string Build(string title, string bodyHtml)
    {
        var year = DateTime.UtcNow.Year;
        var sb = new StringBuilder();

        sb.AppendLine("<!DOCTYPE html>");
        sb.AppendLine("<html lang=\"en\">");
        sb.AppendLine("<head>");
        sb.AppendLine("<meta charset=\"utf-8\">");
        sb.AppendLine("<meta name=\"viewport\" content=\"width=device-width, initial-scale=1\">");
        sb.AppendLine($"<title>{HtmlEncode(title)}</title>");
        sb.AppendLine("</head>");
        sb.AppendLine($"<body style=\"margin:0;padding:0;background:{Bg};font-family:-apple-system,BlinkMacSystemFont,'Segoe UI',Roboto,'Helvetica Neue',Arial,sans-serif;color:{TextDark};\">");

        // Outer wrapper — full-width
        sb.AppendLine($"<table role=\"presentation\" width=\"100%\" cellpadding=\"0\" cellspacing=\"0\" border=\"0\" style=\"background:{Bg};padding:32px 16px;\">");
        sb.AppendLine("<tr><td align=\"center\">");

        // Card — fixed max-width
        sb.AppendLine($"<table role=\"presentation\" width=\"560\" cellpadding=\"0\" cellspacing=\"0\" border=\"0\" style=\"max-width:560px;width:100%;background:{Card};border:1px solid {Border};border-radius:16px;overflow:hidden;\">");

        // Brand header
        sb.AppendLine("<tr>");
        sb.AppendLine($"<td style=\"padding:24px 32px;border-bottom:1px solid {Border};\">");
        sb.AppendLine($"<div style=\"font-weight:700;font-size:18px;color:{TealColor};letter-spacing:-0.01em;\">EduNexis</div>");
        sb.AppendLine($"<div style=\"font-size:11px;color:{TextMuted};margin-top:2px;\">Jashore University of Science and Technology</div>");
        sb.AppendLine("</td>");
        sb.AppendLine("</tr>");

        // Content
        sb.AppendLine("<tr>");
        sb.AppendLine("<td style=\"padding:32px;\">");
        sb.AppendLine($"<h1 style=\"margin:0 0 16px;font-size:22px;font-weight:700;color:{TextDark};letter-spacing:-0.01em;\">{HtmlEncode(title)}</h1>");
        sb.AppendLine($"<div style=\"font-size:15px;line-height:1.65;color:{TextDark};\">");
        sb.AppendLine(bodyHtml);
        sb.AppendLine("</div>");
        sb.AppendLine("</td>");
        sb.AppendLine("</tr>");

        // Footer
        sb.AppendLine("<tr>");
        sb.AppendLine($"<td style=\"padding:20px 32px;background:{Bg};border-top:1px solid {Border};\">");
        sb.AppendLine($"<p style=\"margin:0;font-size:12px;color:{TextMuted};line-height:1.6;\">");
        sb.AppendLine("This is an automated message from EduNexis. Please do not reply to this email.");
        sb.AppendLine("</p>");
        sb.AppendLine($"<p style=\"margin:8px 0 0;font-size:11px;color:{TextMuted};\">");
        sb.AppendLine($"&copy; {year} EduNexis &middot; JUST CSE Department");
        sb.AppendLine("</p>");
        sb.AppendLine("</td>");
        sb.AppendLine("</tr>");

        sb.AppendLine("</table>");
        sb.AppendLine("</td></tr>");
        sb.AppendLine("</table>");
        sb.AppendLine("</body>");
        sb.AppendLine("</html>");

        return sb.ToString();
    }

    public string Button(string url, string label)
    {
        return
            $"<table role=\"presentation\" cellpadding=\"0\" cellspacing=\"0\" border=\"0\" style=\"margin:24px 0;\">" +
            $"<tr><td style=\"border-radius:10px;background:{TealColor};\" align=\"center\">" +
            $"<a href=\"{HtmlEncode(url)}\" style=\"display:inline-block;padding:12px 28px;color:#ffffff;text-decoration:none;font-weight:600;font-size:14px;border-radius:10px;background:{TealColor};\">{HtmlEncode(label)}</a>" +
            $"</td></tr></table>";
    }

    private static string HtmlEncode(string s) =>
        s.Replace("&", "&amp;")
         .Replace("<", "&lt;")
         .Replace(">", "&gt;")
         .Replace("\"", "&quot;");
}