namespace EduNexis.Domain.Interfaces.Repositories;

public interface ITeacherQuotaRepository : IBaseRepository<TeacherQuota>
{
    Task<TeacherQuota?> GetActiveQuotaAsync(Guid teacherId, CancellationToken ct = default);
    Task<IEnumerable<TeacherQuota>> GetByAssignedByAsync(Guid adminId, CancellationToken ct = default);
}
