namespace EduNexis.Infrastructure.Persistence.Repositories;
public class UserProfileRepository : BaseRepository<UserProfile>, IUserProfileRepository
{
    public UserProfileRepository(AppDbContext context) : base(context) { }

    public async Task<UserProfile?> GetByUserIdAsync(Guid userId, CancellationToken ct = default) =>
        await DbSet.FirstOrDefaultAsync(p => p.UserId == userId, ct);

    public async Task<UserProfile?> GetBySlugAsync(string slug, CancellationToken ct = default) =>
        await DbSet.FirstOrDefaultAsync(
            p => p.PublicSlug == slug && p.IsPublicProfile, ct);

    public async Task<bool> IsSlugTakenAsync(string slug, Guid excludeUserId, CancellationToken ct = default) =>
        await DbSet.AnyAsync(p => p.PublicSlug == slug && p.UserId != excludeUserId, ct);

    public async Task<List<UserProfile>> ListPublicTeachersAsync(
        string? department, int page, int pageSize, CancellationToken ct = default)
    {
        // Only return UserProfiles where the linked User is an active Teacher.
        // Cross-table join via subquery on Users.
        var query = DbSet.AsNoTracking()
            .Where(p => p.IsPublicProfile)
            .Where(p => Context.Users.Any(u => u.Id == p.UserId && (u.Role == UserRole.Teacher || u.Role == UserRole.SuperAdmin) && u.IsActive));

        if (!string.IsNullOrWhiteSpace(department))
            query = query.Where(p => p.Department == department);

        return await query
            .OrderBy(p => p.FullName)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(ct);
    }

    public async Task<List<string>> ListPublicDepartmentsAsync(CancellationToken ct = default) =>
        await DbSet.AsNoTracking()
            .Where(p => p.IsPublicProfile)
            .Where(p => Context.Users.Any(u => u.Id == p.UserId && (u.Role == UserRole.Teacher || u.Role == UserRole.SuperAdmin) && u.IsActive))
            .Select(p => p.Department)
            .Distinct()
            .OrderBy(d => d)
            .ToListAsync(ct);
}