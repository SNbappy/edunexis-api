namespace EduNexis.Domain.Interfaces.Repositories;

public interface IUnitOfWork : IDisposable
{
    IUserRepository Users { get; }
    IUserProfileRepository UserProfiles { get; }
    ICourseRepository Courses { get; }
    ICourseMemberRepository CourseMembers { get; }
    IJoinRequestRepository JoinRequests { get; }
    ITeacherQuotaRepository TeacherQuotas { get; }
    IBaseRepository<T> GetRepository<T>() where T : BaseEntity;  // ← ADD THIS

    Task<int> SaveChangesAsync(CancellationToken ct = default);
    Task BeginTransactionAsync(CancellationToken ct = default);
    Task CommitTransactionAsync(CancellationToken ct = default);
    Task RollbackTransactionAsync(CancellationToken ct = default);
}
