using EduNexis.Application.DTOs;

namespace EduNexis.Application.Features.Courses.Queries;

/// <summary>
/// The caller's course-creation allowance, summed across every active grant.
/// If they have never had a grant, this reports the free-tier starter
/// allowance that will be provisioned on their first course creation.
/// </summary>
public record GetMyQuotaQuery(Guid TeacherId) : IQuery<ApiResponse<TeacherQuotaDto>>;

public record TeacherQuotaDto(
    int TotalQuota,
    int UsedQuota,
    int RemainingQuota,
    DateTime AccessStartDate,
    DateTime AccessEndDate,
    bool IsAccessActive,
    bool IsStarterQuota,
    /// <summary>Days until the soonest-expiring active grant lapses.</summary>
    int? ExpiresInDays,
    /// <summary>How many separate active grants make up the total.</summary>
    int ActiveGrantCount
);

public sealed class GetMyQuotaQueryHandler(
    IUnitOfWork uow
) : IQueryHandler<GetMyQuotaQuery, ApiResponse<TeacherQuotaDto>>
{
    private const int StarterCourseCount = 1;
    private const int StarterAccessYears = 100;

    public async ValueTask<ApiResponse<TeacherQuotaDto>> Handle(
        GetMyQuotaQuery query, CancellationToken ct)
    {
        var all = await uow.TeacherQuotas.GetAllGrantsAsync(query.TeacherId, ct);
        var active = all.Where(g => g.IsAccessActive).ToList();
        var now = DateTime.UtcNow;

        if (active.Count == 0)
        {
            // Nothing active. If they have never held a grant, show the starter
            // allowance they are about to receive; otherwise show a spent state
            // so the UI can tell them to contact an admin.
            var neverGranted = all.Count == 0;

            return ApiResponse<TeacherQuotaDto>.Ok(new TeacherQuotaDto(
                TotalQuota:      neverGranted ? StarterCourseCount : 0,
                UsedQuota:       0,
                RemainingQuota:  neverGranted ? StarterCourseCount : 0,
                AccessStartDate: now,
                AccessEndDate:   neverGranted ? now.AddYears(StarterAccessYears) : now,
                IsAccessActive:  neverGranted,
                IsStarterQuota:  neverGranted,
                ExpiresInDays:   null,
                ActiveGrantCount: 0
            ));
        }

        var nextExpiry = active.Min(g => g.AccessEndDate);

        return ApiResponse<TeacherQuotaDto>.Ok(new TeacherQuotaDto(
            TotalQuota:      active.Sum(g => g.TotalQuota),
            UsedQuota:       active.Sum(g => g.UsedQuota),
            RemainingQuota:  active.Sum(g => g.RemainingQuota),
            AccessStartDate: active.Min(g => g.AccessStartDate),
            AccessEndDate:   nextExpiry,
            IsAccessActive:  true,
            IsStarterQuota:  active.All(g => g.IsStarterGrant),
            ExpiresInDays:   Math.Max(0, (int)Math.Ceiling((nextExpiry - now).TotalDays)),
            ActiveGrantCount: active.Count
        ));
    }
}
