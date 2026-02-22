namespace EduNexis.Infrastructure.Persistence.Repositories;

public class CourseMemberRepository : BaseRepository<CourseMember>, ICourseMemberRepository
{
    public CourseMemberRepository(AppDbContext context) : base(context) { }

    public async Task<CourseMember?> GetMemberAsync(
        Guid courseId, Guid userId, CancellationToken ct = default) =>
        await DbSet.FirstOrDefaultAsync(m =>
            m.CourseId == courseId && m.UserId == userId && m.IsActive, ct);

    public async Task<IEnumerable<CourseMember>> GetByCourseAsync(
        Guid courseId, CancellationToken ct = default) =>
        await DbSet.Where(m => m.CourseId == courseId && m.IsActive).ToListAsync(ct);

    public async Task<IEnumerable<CourseMember>> GetCRsAsync(
        Guid courseId, CancellationToken ct = default) =>
        await DbSet.Where(m => m.CourseId == courseId && m.IsCR && m.IsActive).ToListAsync(ct);
}
