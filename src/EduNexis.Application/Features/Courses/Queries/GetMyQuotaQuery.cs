using EduNexis.Application.DTOs;

namespace EduNexis.Application.Features.Courses.Queries;

/// <summary>
/// Returns the caller's current teacher-quota status. If no quota row exists
/// yet (teacher hasn't created their first course), returns the implicit
/// starter state: 1 course allowed, 0 used, access active.
/// </summary>
public record GetMyQuotaQuery(Guid TeacherId) : IQuery<ApiResponse<TeacherQuotaDto>>;

public record TeacherQuotaDto(
    int TotalQuota,
    int UsedQuota,
    int RemainingQuota,
    DateTime AccessStartDate,
    DateTime AccessEndDate,
    bool IsAccessActive,
    bool IsStarterQuota
);

public sealed class GetMyQuotaQueryHandler(
    IUnitOfWork uow
) : IQueryHandler<GetMyQuotaQuery, ApiResponse<TeacherQuotaDto>>
{
    private const int StarterCourseCount = 1;

    public async ValueTask<ApiResponse<TeacherQuotaDto>> Handle(
        GetMyQuotaQuery query, CancellationToken ct)
    {
        var quota = await uow.TeacherQuotas.GetActiveQuotaAsync(query.TeacherId, ct);

        if (quota is null)
        {
            // No quota row yet — reflect the implicit starter grant that
            // CreateCourseCommandHandler will auto-provision on first creation.
            var now = DateTime.UtcNow;
            return ApiResponse<TeacherQuotaDto>.Ok(new TeacherQuotaDto(
                TotalQuota:      StarterCourseCount,
                UsedQuota:       0,
                RemainingQuota:  StarterCourseCount,
                AccessStartDate: now,
                AccessEndDate:   now.AddYears(100),
                IsAccessActive:  true,
                IsStarterQuota:  true
            ));
        }

        return ApiResponse<TeacherQuotaDto>.Ok(new TeacherQuotaDto(
            TotalQuota:      quota.TotalQuota,
            UsedQuota:       quota.UsedQuota,
            RemainingQuota:  quota.RemainingQuota,
            AccessStartDate: quota.AccessStartDate,
            AccessEndDate:   quota.AccessEndDate,
            IsAccessActive:  quota.IsAccessActive,
            IsStarterQuota:  quota.AssignedById == quota.TeacherId
        ));
    }
}