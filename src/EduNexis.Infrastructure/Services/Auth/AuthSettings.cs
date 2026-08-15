using EduNexis.Application.Abstractions;
using Microsoft.Extensions.Configuration;

namespace EduNexis.Infrastructure.Services.Auth;

public class AuthSettings : IAuthSettings
{
    public bool OtpRequired { get; }

    public IReadOnlySet<string> AdminEmails { get; }

    public AuthSettings(IConfiguration configuration)
    {
        OtpRequired = configuration.GetValue<bool>("Auth:OtpRequired", true);
        AdminEmails = ReadAdminEmails(configuration);
    }

    /// <summary>
    /// Accepts either shape, because the two hosting styles disagree:
    /// appsettings.json is natural as a JSON array, while env-var hosts such as
    /// Render can only supply a flat string (Auth__AdminEmails=a@x,b@y).
    /// </summary>
    private static IReadOnlySet<string> ReadAdminEmails(IConfiguration configuration)
    {
        var section = configuration.GetSection("Auth:AdminEmails");

        // JSON array form: Auth:AdminEmails:0, :1, …
        var values = section.GetChildren().Select(c => c.Value).ToList();

        // Flat string form: "a@x.edu, b@y.edu"
        if (values.Count == 0 && !string.IsNullOrWhiteSpace(section.Value))
            values = section.Value.Split(',').Cast<string?>().ToList();

        return values
            .Where(v => !string.IsNullOrWhiteSpace(v))
            .Select(v => v!.Trim())
            .ToHashSet(StringComparer.OrdinalIgnoreCase);
    }
}
