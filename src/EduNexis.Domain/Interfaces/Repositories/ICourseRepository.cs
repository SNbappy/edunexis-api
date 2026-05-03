using EduNexis.Domain.Entities;

namespace EduNexis.Domain.Interfaces.Repositories;

public interface ICourseRepository : IBaseRepository<Course>
{
    new Task<Course?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<Course?> GetByJoiningCodeAsync(string code, CancellationToken ct = default);
    Task<Course?> GetWithMembersAsync(Guid courseId, CancellationToken ct = default);
    Task<IEnumerable<Course>> GetByTeacherAsync(Guid teacherId, CancellationToken ct = default);
    Task<IEnumerable<Course>> GetByStudentAsync(Guid studentId, CancellationToken ct = default);
    new Task<IEnumerable<Course>> GetAllAsync(CancellationToken ct = default);

    /// <summary>Active (non-archived) course counts for a set of teacher IDs.</summary>
    Task<Dictionary<Guid, int>> GetActiveCountsByTeacherIdsAsync(
        IEnumerable<Guid> teacherIds, CancellationToken ct = default);

    /// <summary>Total non-archived courses across the platform.</summary>
    Task<int> CountActiveAsync(CancellationToken ct = default);
}