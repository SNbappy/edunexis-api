namespace EduNexis.Infrastructure.Persistence.Repositories;

public class UserRepository : BaseRepository<User>, IUserRepository
{
    public UserRepository(AppDbContext context) : base(context) { }

    public async Task<User?> GetByFirebaseUidAsync(string uid, CancellationToken ct = default) =>
        await DbSet.Include(u => u.Profile)
            .FirstOrDefaultAsync(u => u.FirebaseUid == uid, ct);

    public async Task<User?> GetByEmailAsync(string email, CancellationToken ct = default) =>
        await DbSet.FirstOrDefaultAsync(u => u.Email == email.ToLowerInvariant(), ct);

    public async Task<User?> GetWithProfileAsync(Guid userId, CancellationToken ct = default) =>
        await DbSet.Include(u => u.Profile)
            .FirstOrDefaultAsync(u => u.Id == userId, ct);
}
