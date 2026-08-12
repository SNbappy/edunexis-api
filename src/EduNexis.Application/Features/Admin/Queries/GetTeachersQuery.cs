using EduNexis.Domain.Entities;
using EduNexis.Domain.Enums;

namespace EduNexis.Application.Features.Admin.Queries;

public record TeacherAdminDto(
    Guid Id,
    string Email,
    string? FullName,
    int ActiveCourseCount,
    int? TotalQuota,
    int? UsedQuota,
    int? RemainingQuota,
    bool HasActiveQuota
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

        var quotas = await uow.GetRepository<TeacherQuota>()
            .FindAsync(q => teacherIds.Contains(q.TeacherId), ct);
        var quotaByTeacher = quotas
            .Where(q => q.IsAccessActive)
            .GroupBy(q => q.TeacherId)
            .ToDictionary(g => g.Key, g => g.OrderByDescending(q => q.CreatedAt).First());

        var profiles = await uow.UserProfiles.FindAsync(p => teacherIds.Contains(p.UserId), ct);
        var profileByUser = profiles.ToDictionary(p => p.UserId, p => p.FullName);

        var dtos = teacherList.Select(t =>
        {
            quotaByTeacher.TryGetValue(t.Id, out var quota);
            profileByUser.TryGetValue(t.Id, out var fullName);
            activeCounts.TryGetValue(t.Id, out var activeCount);

            return new TeacherAdminDto(
                Id: t.Id,
                Email: t.Email,
                FullName: fullName,
                ActiveCourseCount: activeCount,
                TotalQuota: quota?.TotalQuota,
                UsedQuota: quota?.UsedQuota,
                RemainingQuota: quota?.RemainingQuota,
                HasActiveQuota: quota is not null
            );
        })
        .OrderByDescending(t => t.ActiveCourseCount)
        .ToList();

        return ApiResponse<List<TeacherAdminDto>>.Ok(dtos);
    }
}