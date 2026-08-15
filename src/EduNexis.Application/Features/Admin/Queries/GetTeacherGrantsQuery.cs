namespace EduNexis.Application.Features.Admin.Queries;

/// <summary>
/// Full grant history for one teacher, newest first — including spent, expired
/// and revoked grants, so an admin can see what was given and when before
/// deciding whether to give more.
/// </summary>
public record GrantDto(
    Guid Id,
    int Courses,
    int Used,
    int Remaining,
    DateTime StartsAt,
    DateTime ExpiresAt,
    int ExpiresInDays,
    bool IsActive,
    bool IsRevoked,
    bool IsStarterGrant,
    string? Note,
    string? GrantedByEmail,
    DateTime GrantedAt,
    /// <summary>active | used-up | expired | revoked — one word for the UI.</summary>
    string Status
);

public record GetTeacherGrantsQuery(Guid TeacherId) : IQuery<ApiResponse<List<GrantDto>>>;

public sealed class GetTeacherGrantsQueryHandler(
    IUnitOfWork uow
) : IQueryHandler<GetTeacherGrantsQuery, ApiResponse<List<GrantDto>>>
{
    public async ValueTask<ApiResponse<List<GrantDto>>> Handle(
        GetTeacherGrantsQuery query, CancellationToken ct)
    {
        var grants = await uow.TeacherQuotas.GetAllGrantsAsync(query.TeacherId, ct);

        var adminIds = grants.Select(g => g.AssignedById).Distinct().ToList();
        var admins = await uow.Users.FindAsync(u => adminIds.Contains(u.Id), ct);
        var adminEmail = admins.ToDictionary(u => u.Id, u => u.Email);

        var now = DateTime.UtcNow;

        var dtos = grants.Select(g =>
        {
            adminEmail.TryGetValue(g.AssignedById, out var email);

            // Order matters: revoked wins over expired, expired over used-up,
            // so the label always names the reason it cannot be spent.
            var status =
                g.IsRevoked ? "revoked"
                : now > g.AccessEndDate ? "expired"
                : g.RemainingQuota <= 0 ? "used-up"
                : "active";

            return new GrantDto(
                Id: g.Id,
                Courses: g.TotalQuota,
                Used: g.UsedQuota,
                Remaining: g.RemainingQuota,
                StartsAt: g.AccessStartDate,
                ExpiresAt: g.AccessEndDate,
                ExpiresInDays: Math.Max(0, (int)Math.Ceiling((g.AccessEndDate - now).TotalDays)),
                IsActive: g.IsAccessActive,
                IsRevoked: g.IsRevoked,
                IsStarterGrant: g.IsStarterGrant,
                Note: g.Note,
                GrantedByEmail: g.IsStarterGrant ? null : email,
                GrantedAt: g.CreatedAt,
                Status: status);
        }).ToList();

        return ApiResponse<List<GrantDto>>.Ok(dtos);
    }
}
