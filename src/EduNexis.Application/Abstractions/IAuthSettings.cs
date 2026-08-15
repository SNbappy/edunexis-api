namespace EduNexis.Application.Abstractions;

/// <summary>
/// Auth-related runtime settings exposed to Application layer.
/// Implementation in Infrastructure reads from IConfiguration.
/// </summary>
public interface IAuthSettings
{
    /// <summary>
    /// Whether email OTP verification is required at login.
    /// Defaults to true. Can be set to false via Auth:OtpRequired=false
    /// as a defense-day safety valve if email infrastructure fails.
    /// </summary>
    bool OtpRequired { get; }

    /// <summary>
    /// Emails bootstrapped to SuperAdmin on successful login.
    /// Empty by default — admin is opt-in, per environment.
    ///
    /// Set via <c>Auth:AdminEmails</c>: a comma-separated string, or a JSON
    /// array in appsettings. On env-var hosts such as Render that is
    /// <c>Auth__AdminEmails=admin@just.edu.bd</c>.
    ///
    /// This was previously a hardcoded HashSet inside LoginUserCommandHandler
    /// holding one teacher and one student address, which is how a student
    /// account came to hold SuperAdmin. Configuration makes it a real
    /// deploy-time decision instead of something compiled into the binary.
    /// </summary>
    IReadOnlySet<string> AdminEmails { get; }
}