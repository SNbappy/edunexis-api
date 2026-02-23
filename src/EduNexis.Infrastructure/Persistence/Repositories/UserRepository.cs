using EduNexis.Domain.Entities;
using EduNexis.Domain.Interfaces.Repositories;
using Microsoft.EntityFrameworkCore;

namespace EduNexis.Infrastructure.Persistence.Repositories;

public class UserRepository : BaseRepository<User>, IUserRepository
{
    public UserRepository(AppDbContext context) : base(context) { }

    public async Task<User?> GetByEmailAsync(string email, CancellationToken ct = default) =>
        await DbSet
            .Include(u => u.Profile)
            .FirstOrDefaultAsync(u => u.Email == email.ToLowerInvariant(), ct);

    public async Task<User?> GetByRefreshTokenAsync(string refreshToken, CancellationToken ct = default) =>
        await DbSet
            .Include(u => u.Profile)
            .FirstOrDefaultAsync(u =>
                u.RefreshToken == refreshToken &&
                u.RefreshTokenExpiry.HasValue &&
                u.RefreshTokenExpiry > DateTime.UtcNow,
                ct);

    public async Task<User?> GetWithProfileAsync(Guid userId, CancellationToken ct = default) =>
        await DbSet
            .Include(u => u.Profile)
            .FirstOrDefaultAsync(u => u.Id == userId, ct);
}
