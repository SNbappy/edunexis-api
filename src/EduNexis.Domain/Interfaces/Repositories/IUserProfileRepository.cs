namespace EduNexis.Domain.Interfaces.Repositories;

public interface IUserProfileRepository : IBaseRepository<UserProfile>
{
    Task<UserProfile?> GetByUserIdAsync(Guid userId, CancellationToken ct = default);
}
