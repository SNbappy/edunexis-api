namespace EduNexis.Infrastructure.Persistence.Repositories;

public class CourseRepository : BaseRepository<Course>, ICourseRepository
{
    public CourseRepository(AppDbContext context) : base(context) { }

    public new async Task<Course?> GetByIdAsync(Guid id, CancellationToken ct = default) =>
        await DbSet
            .Include(c => c.Teacher).ThenInclude(t => t.Profile)
            .Include(c => c.Members).ThenInclude(m => m.User).ThenInclude(u => u.Profile)
            .FirstOrDefaultAsync(c => c.Id == id, ct);

    public async Task<Course?> GetByJoiningCodeAsync(string code, CancellationToken ct = default) =>
        await DbSet
            .Include(c => c.Teacher).ThenInclude(t => t.Profile)
            .Include(c => c.Members)
            .FirstOrDefaultAsync(c => c.JoiningCode == code, ct);

    public async Task<Course?> GetWithMembersAsync(Guid courseId, CancellationToken ct = default) =>
        await DbSet
            .Include(c => c.Members).ThenInclude(m => m.User).ThenInclude(u => u.Profile)
            .FirstOrDefaultAsync(c => c.Id == courseId, ct);

    public async Task<IEnumerable<Course>> GetByTeacherAsync(Guid teacherId, CancellationToken ct = default) =>
        await DbSet
            .Include(c => c.Teacher).ThenInclude(t => t.Profile)
            .Include(c => c.Members)
            .Where(c => c.TeacherId == teacherId)
            .OrderByDescending(c => c.CreatedAt)
            .ToListAsync(ct);

    public async Task<IEnumerable<Course>> GetByStudentAsync(Guid studentId, CancellationToken ct = default) =>
        await DbSet
            .Include(c => c.Teacher).ThenInclude(t => t.Profile)
            .Include(c => c.Members)
            .Where(c => c.Members.Any(m => m.UserId == studentId && m.IsActive))
            .OrderByDescending(c => c.CreatedAt)
            .ToListAsync(ct);

    public new async Task<IEnumerable<Course>> GetAllAsync(CancellationToken ct = default) =>
        await DbSet
            .Include(c => c.Teacher).ThenInclude(t => t.Profile)
            .Include(c => c.Members)
            .OrderByDescending(c => c.CreatedAt)
            .ToListAsync(ct);
public async Task<Dictionary<Guid, int>> GetActiveCountsByTeacherIdsAsync(
        IEnumerable<Guid> teacherIds, CancellationToken ct = default)
    {
        var ids = teacherIds.ToList();
        return await DbSet.AsNoTracking()
            .Where(c => ids.Contains(c.TeacherId) && !c.IsArchived
                && !c.IsDeletedByOwner && !c.IsDeleted)
            .GroupBy(c => c.TeacherId)
            .Select(g => new { TeacherId = g.Key, Count = g.Count() })
            .ToDictionaryAsync(x => x.TeacherId, x => x.Count, ct);
    }

    public async Task<int> CountActiveAsync(CancellationToken ct = default) =>
        await DbSet.AsNoTracking().CountAsync(
            c => !c.IsArchived && !c.IsDeletedByOwner && !c.IsDeleted, ct);

    public async Task<IEnumerable<Course>> GetDeletedByTeacherAsync(Guid teacherId, CancellationToken ct = default) =>
        await DbSet
            .Where(c => c.TeacherId == teacherId && c.IsDeletedByOwner)
            .OrderByDescending(c => c.DeletedByOwnerAt)
            .ToListAsync(ct);
}