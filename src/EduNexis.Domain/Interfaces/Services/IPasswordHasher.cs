// src/EduNexis.Domain/Interfaces/Services/IPasswordHasher.cs
namespace EduNexis.Domain.Interfaces.Services;

public interface IPasswordHasher
{
    string Hash(string password);
    bool Verify(string password, string hash);
}
