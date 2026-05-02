namespace EduNexis.Domain.Interfaces.Services;

public interface IResetTokenGenerator
{
    /// <summary>
    /// Generates a cryptographically random 32-byte token, base64url-encoded.
    /// Returns plaintext (to email) and hash (to store in DB).
    /// </summary>
    (string PlainToken, string Hash) Generate();
}