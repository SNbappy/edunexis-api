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
}