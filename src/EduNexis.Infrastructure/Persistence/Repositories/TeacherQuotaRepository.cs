namespace EduNexis.Infrastructure.Persistence.Repositories;

public class TeacherQuotaRepository : BaseRepository<TeacherQuota>, ITeacherQuotaRepository
{
    public TeacherQuotaRepository(AppDbContext context) : base(context) { }

    public async Task<TeacherQuota?> GetActiveQuotaAsync(Guid teacherId, CancellationToken ct = default) =>
        await DbSet.FirstOrDefaultAsync(q =>
            q.TeacherId == teacherId &&
            q.AccessStartDate <= DateTime.UtcNow &&
            q.AccessEndDate >= DateTime.UtcNow, ct);

    public async Task<IEnumerable<TeacherQuota>> GetByAssignedByAsync(
        Guid adminId, CancellationToken ct = default) =>
        await DbSet.Where(q => q.AssignedById == adminId)
            .OrderByDescending(q => q.CreatedAt)
            .ToListAsync(ct);
}
