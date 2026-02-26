namespace EduNexis.Infrastructure.Persistence.Repositories;

public class CourseMemberRepository : BaseRepository<CourseMember>, ICourseMemberRepository
{
    public CourseMemberRepository(AppDbContext context) : base(context) { }

    public async Task<CourseMember?> GetMemberAsync(
        Guid courseId, Guid userId, CancellationToken ct = default) =>
        await DbSet.FirstOrDefaultAsync(m =>
            m.CourseId == courseId && m.UserId == userId, ct);

    public async Task<IEnumerable<CourseMember>> GetByCourseAsync(
        Guid courseId, CancellationToken ct = default) =>
        await DbSet
            .Include(m => m.User).ThenInclude(u => u.Profile)
            .Where(m => m.CourseId == courseId && m.IsActive)
            .OrderBy(m => m.User.Profile != null ? m.User.Profile.FullName : string.Empty)
            .ToListAsync(ct);

    public async Task<IEnumerable<CourseMember>> GetCRsAsync(
        Guid courseId, CancellationToken ct = default) =>
        await DbSet
            .Include(m => m.User).ThenInclude(u => u.Profile)
            .Where(m => m.CourseId == courseId && m.IsCR && m.IsActive)
            .ToListAsync(ct);
}
