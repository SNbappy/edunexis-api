namespace EduNexis.Domain.Interfaces.Repositories;
public interface IUserProfileRepository : IBaseRepository<UserProfile>
{
    Task<UserProfile?> GetByUserIdAsync(Guid userId, CancellationToken ct = default);

    /// <summary>
    /// Find a profile by its public slug (used for /faculty/{slug} lookups).
    /// Returns null if not found OR if the profile is private.
    /// </summary>
    Task<UserProfile?> GetBySlugAsync(string slug, CancellationToken ct = default);

    /// <summary>
    /// Check if a slug is already taken (excluding the user themselves so that
    /// an existing public teacher can re-save without false collisions).
    /// </summary>
    Task<bool> IsSlugTakenAsync(string slug, Guid excludeUserId, CancellationToken ct = default);
}