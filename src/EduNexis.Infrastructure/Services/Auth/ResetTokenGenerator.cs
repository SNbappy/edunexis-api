using EduNexis.Domain.Interfaces.Services;
using System.Security.Cryptography;

namespace EduNexis.Infrastructure.Services.Auth;

public class ResetTokenGenerator(IPasswordHasher passwordHasher) : IResetTokenGenerator
{
    public (string PlainToken, string Hash) Generate()
    {
        // 32 random bytes => 256-bit security, base64url-safe for URLs
        var bytes = new byte[32];
        RandomNumberGenerator.Fill(bytes);
        var plain = Convert.ToBase64String(bytes)
            .Replace('+', '-')
            .Replace('/', '_')
            .TrimEnd('=');

        var hash = passwordHasher.Hash(plain);
        return (plain, hash);
    }
}