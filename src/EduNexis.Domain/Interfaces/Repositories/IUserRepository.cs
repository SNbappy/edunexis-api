using EduNexis.Domain.Entities;

namespace EduNexis.Domain.Interfaces.Repositories;

public interface IUserRepository : IBaseRepository<User>
{
    Task<User?> GetByEmailAsync(string email, CancellationToken ct = default);
    Task<User?> GetByRefreshTokenAsync(string refreshToken, CancellationToken ct = default);
    Task<User?> GetWithProfileAsync(Guid userId, CancellationToken ct = default);
}
