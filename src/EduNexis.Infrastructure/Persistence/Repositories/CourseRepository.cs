namespace EduNexis.Infrastructure.Persistence.Repositories;

public class CourseRepository : BaseRepository<Course>, ICourseRepository
{
    public CourseRepository(AppDbContext context) : base(context) { }

    public async Task<Course?> GetByJoiningCodeAsync(string code, CancellationToken ct = default) =>
        await DbSet.FirstOrDefaultAsync(c => c.JoiningCode == code, ct);

    public async Task<Course?> GetWithMembersAsync(Guid courseId, CancellationToken ct = default) =>
        await DbSet.Include(c => c.Members)
            .FirstOrDefaultAsync(c => c.Id == courseId, ct);

    public async Task<IEnumerable<Course>> GetByTeacherAsync(Guid teacherId, CancellationToken ct = default) =>
        await DbSet.Where(c => c.TeacherId == teacherId)
            .OrderByDescending(c => c.CreatedAt)
            .ToListAsync(ct);

    public async Task<IEnumerable<Course>> GetByStudentAsync(Guid studentId, CancellationToken ct = default) =>
        await DbSet
            .Where(c => c.Members.Any(m => m.UserId == studentId && m.IsActive))
            .OrderByDescending(c => c.CreatedAt)
            .ToListAsync(ct);
}
