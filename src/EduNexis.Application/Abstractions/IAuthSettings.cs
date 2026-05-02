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
}