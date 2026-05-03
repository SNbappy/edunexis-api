namespace EduNexis.Domain.Interfaces.Repositories;
public interface IUserProfileRepository : IBaseRepository<UserProfile>
{
    Task<UserProfile?> GetByUserIdAsync(Guid userId, CancellationToken ct = default);
    Task<UserProfile?> GetBySlugAsync(string slug, CancellationToken ct = default);
    Task<bool> IsSlugTakenAsync(string slug, Guid excludeUserId, CancellationToken ct = default);

    /// <summary>List public teacher profiles, paginated and optionally department-filtered.</summary>
    Task<List<UserProfile>> ListPublicTeachersAsync(
        string? department, int page, int pageSize, CancellationToken ct = default);

    /// <summary>Distinct departments that have at least one public teacher.</summary>
    Task<List<string>> ListPublicDepartmentsAsync(CancellationToken ct = default);
}