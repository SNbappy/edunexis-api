namespace EduNexis.Domain.Interfaces.Repositories;

public interface ICourseRepository : IBaseRepository<Course>
{
    Task<Course?> GetByJoiningCodeAsync(string code, CancellationToken ct = default);
    Task<Course?> GetWithMembersAsync(Guid courseId, CancellationToken ct = default);
    Task<IEnumerable<Course>> GetByTeacherAsync(Guid teacherId, CancellationToken ct = default);
    Task<IEnumerable<Course>> GetByStudentAsync(Guid studentId, CancellationToken ct = default);
}
