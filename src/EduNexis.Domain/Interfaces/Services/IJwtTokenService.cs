namespace EduNexis.Domain.Interfaces.Services;

public interface IJwtTokenService
{
    string GenerateAccessToken(Guid userId, string email, string role);
    string GenerateRefreshToken();
}
