namespace EduNexis.Infrastructure.Persistence.Repositories;

public class JoinRequestRepository : BaseRepository<JoinRequest>, IJoinRequestRepository
{
    public JoinRequestRepository(AppDbContext context) : base(context) { }

    public async Task<JoinRequest?> GetPendingAsync(
        Guid courseId, Guid studentId, CancellationToken ct = default) =>
        await DbSet.FirstOrDefaultAsync(j =>
            j.CourseId == courseId &&
            j.StudentId == studentId &&
            j.Status == JoinRequestStatus.Pending, ct);

    public async Task<IEnumerable<JoinRequest>> GetPendingByCourseAsync(
        Guid courseId, CancellationToken ct = default) =>
        await DbSet
            .Where(j => j.CourseId == courseId && j.Status == JoinRequestStatus.Pending)
            .OrderByDescending(j => j.CreatedAt)
            .ToListAsync(ct);
}
