namespace EduNexis.Domain.Interfaces.Repositories;

public interface IJoinRequestRepository : IBaseRepository<JoinRequest>
{
    Task<JoinRequest?> GetPendingAsync(Guid courseId, Guid studentId, CancellationToken ct = default);
    Task<IEnumerable<JoinRequest>> GetPendingByCourseAsync(Guid courseId, CancellationToken ct = default);
}
