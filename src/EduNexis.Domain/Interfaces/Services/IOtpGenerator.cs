namespace EduNexis.Domain.Interfaces.Services;

public interface IOtpGenerator
{
    /// <summary>
    /// Generates a cryptographically random 6-digit numeric OTP.
    /// Returns plaintext (to email) and hash (to store in DB).
    /// </summary>
    (string PlainOtp, string Hash) Generate();
}