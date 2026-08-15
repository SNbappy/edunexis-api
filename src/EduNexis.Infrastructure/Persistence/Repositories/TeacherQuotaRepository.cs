namespace EduNexis.Infrastructure.Persistence.Repositories;

public class TeacherQuotaRepository : BaseRepository<TeacherQuota>, ITeacherQuotaRepository
{
    public TeacherQuotaRepository(AppDbContext context) : base(context) { }

    public async Task<List<TeacherQuota>> GetSpendableGrantsAsync(
        Guid teacherId, CancellationToken ct = default) =>
        await DbSet
            .Where(q => q.TeacherId == teacherId
                     && q.RevokedAt == null
                     && q.AccessStartDate <= DateTime.UtcNow
                     && q.AccessEndDate >= DateTime.UtcNow
                     && q.UsedQuota < q.TotalQuota)
            // Soonest expiry first, so nothing is left to lapse unused.
            .OrderBy(q => q.AccessEndDate)
            .ToListAsync(ct);

    public async Task<List<TeacherQuota>> GetAllGrantsAsync(
        Guid teacherId, CancellationToken ct = default) =>
        await DbSet
            .Where(q => q.TeacherId == teacherId)
            .OrderByDescending(q => q.CreatedAt)
            .ToListAsync(ct);

    public async Task<List<TeacherQuota>> GetActiveGrantsForTeachersAsync(
        IEnumerable<Guid> teacherIds, CancellationToken ct = default)
    {
        var ids = teacherIds.ToList();
        return await DbSet
            .Where(q => ids.Contains(q.TeacherId)
                     && q.RevokedAt == null
                     && q.AccessStartDate <= DateTime.UtcNow
                     && q.AccessEndDate >= DateTime.UtcNow)
            .ToListAsync(ct);
    }

    public async Task<IEnumerable<TeacherQuota>> GetByAssignedByAsync(
        Guid adminId, CancellationToken ct = default) =>
        await DbSet.Where(q => q.AssignedById == adminId)
            .OrderByDescending(q => q.CreatedAt)
            .ToListAsync(ct);
}
