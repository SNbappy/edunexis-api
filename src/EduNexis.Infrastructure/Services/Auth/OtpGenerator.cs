using EduNexis.Domain.Interfaces.Services;
using System.Security.Cryptography;

namespace EduNexis.Infrastructure.Services.Auth;

public class OtpGenerator(IPasswordHasher passwordHasher) : IOtpGenerator
{
    public (string PlainOtp, string Hash) Generate()
    {
        // Cryptographic RNG, 6 digits => 0..999_999, zero-padded
        var bytes = new byte[4];
        RandomNumberGenerator.Fill(bytes);
        var num = BitConverter.ToUInt32(bytes, 0) % 1_000_000u;
        var plain = num.ToString("D6");
        var hash = passwordHasher.Hash(plain);
        return (plain, hash);
    }
}