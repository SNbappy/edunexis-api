namespace EduNexis.Domain.Interfaces.Repositories;

public interface ITeacherQuotaRepository : IBaseRepository<TeacherQuota>
{
    /// <summary>
    /// Grants that can still be spent, soonest expiry first.
    ///
    /// The order is the policy: spending the grant that expires first means a
    /// teacher never loses allowance they could have used, which is the whole
    /// point of tracking grants separately.
    /// </summary>
    Task<List<TeacherQuota>> GetSpendableGrantsAsync(Guid teacherId, CancellationToken ct = default);

    /// <summary>
    /// Every grant ever issued to a teacher, newest first — including expired
    /// and revoked ones, so the admin can see the history.
    /// </summary>
    Task<List<TeacherQuota>> GetAllGrantsAsync(Guid teacherId, CancellationToken ct = default);

    /// <summary>All currently-active grants for a set of teachers, for list views.</summary>
    Task<List<TeacherQuota>> GetActiveGrantsForTeachersAsync(
        IEnumerable<Guid> teacherIds, CancellationToken ct = default);

    Task<IEnumerable<TeacherQuota>> GetByAssignedByAsync(Guid adminId, CancellationToken ct = default);
}
