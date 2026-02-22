namespace EduNexis.Domain.Interfaces.Repositories;

public interface ICourseMemberRepository : IBaseRepository<CourseMember>
{
    Task<CourseMember?> GetMemberAsync(Guid courseId, Guid userId, CancellationToken ct = default);
    Task<IEnumerable<CourseMember>> GetByCourseAsync(Guid courseId, CancellationToken ct = default);
    Task<IEnumerable<CourseMember>> GetCRsAsync(Guid courseId, CancellationToken ct = default);
}
