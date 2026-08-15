using EduNexis.Domain.Entities;
using EduNexis.Domain.Enums;

namespace EduNexis.Application.Features.Admin.Queries;

/// <summary>
/// Quota figures are aggregated across every active grant a teacher holds,
/// rather than read off one row. <see cref="ExpiresInDays"/> counts down to the
/// grant that lapses first, which is the date that actually matters to them.
/// </summary>
public record TeacherAdminDto(
    Guid Id,
    string Email,
    string? FullName,
    int ActiveCourseCount,
    int? TotalQuota,
    int? UsedQuota,
    int? RemainingQuota,
    bool HasActiveQuota,
    DateTime? NextExpiryDate,
    int? ExpiresInDays,
    int ActiveGrantCount
);

public record GetTeachersQuery : IQuery<ApiResponse<List<TeacherAdminDto>>>;

public sealed class GetTeachersQueryHandler(
    IUnitOfWork uow
) : IQueryHandler<GetTeachersQuery, ApiResponse<List<TeacherAdminDto>>>
{
    public async ValueTask<ApiResponse<List<TeacherAdminDto>>> Handle(
        GetTeachersQuery query, CancellationToken ct)
    {
        var teachers = await uow.GetRepository<User>().FindAsync(u => u.Role == UserRole.Teacher, ct);
        var teacherList = teachers.ToList();
        var teacherIds = teacherList.Select(t => t.Id).ToList();

        var activeCounts = await uow.Courses.GetActiveCountsByTeacherIdsAsync(teacherIds, ct);

        var activeGrants = await uow.TeacherQuotas.GetActiveGrantsForTeachersAsync(teacherIds, ct);
        var grantsByTeacher = activeGrants
            .GroupBy(g => g.TeacherId)
            .ToDictionary(g => g.Key, g => g.ToList());

        var profiles = await uow.UserProfiles.FindAsync(p => teacherIds.Contains(p.UserId), ct);
        var profileByUser = profiles.ToDictionary(p => p.UserId, p => p.FullName);

        var now = DateTime.UtcNow;

        var dtos = teacherList.Select(t =>
        {
            profileByUser.TryGetValue(t.Id, out var fullName);
            activeCounts.TryGetValue(t.Id, out var activeCount);
            grantsByTeacher.TryGetValue(t.Id, out var grants);

            var has = grants is { Count: > 0 };
            DateTime? nextExpiry = has ? grants!.Min(g => g.AccessEndDate) : null;

            return new TeacherAdminDto(
                Id: t.Id,
                Email: t.Email,
                FullName: fullName,
                ActiveCourseCount: activeCount,
                TotalQuota: has ? grants!.Sum(g => g.TotalQuota) : null,
                UsedQuota: has ? grants!.Sum(g => g.UsedQuota) : null,
                RemainingQuota: has ? grants!.Sum(g => g.RemainingQuota) : null,
                HasActiveQuota: has,
                NextExpiryDate: nextExpiry,
                // Rounded up, so "expires today" reads as 1 day rather than 0.
                ExpiresInDays: nextExpiry is null
                    ? null
                    : Math.Max(0, (int)Math.Ceiling((nextExpiry.Value - now).TotalDays)),
                ActiveGrantCount: has ? grants!.Count : 0
            );
        })
        .OrderByDescending(t => t.ActiveCourseCount)
        .ToList();

        return ApiResponse<List<TeacherAdminDto>>.Ok(dtos);
    }
}
