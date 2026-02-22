namespace EduNexis.Domain.Interfaces.Repositories;

public interface IUserRepository : IBaseRepository<User>
{
    Task<User?> GetByFirebaseUidAsync(string firebaseUid, CancellationToken ct = default);
    Task<User?> GetByEmailAsync(string email, CancellationToken ct = default);
    Task<User?> GetWithProfileAsync(Guid userId, CancellationToken ct = default);
}
