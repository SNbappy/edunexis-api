using EduNexis.Application.Abstractions;
using EduNexis.Application.DTOs;
using EduNexis.Application.Extensions;

namespace EduNexis.Application.Features.Courses.Queries;

/// <summary>
/// Admin-only course listing. Regular users should use GetMyCoursesQuery.
/// </summary>
public record GetCoursesQuery(
    Guid? TeacherId = null,
    Guid? StudentId = null
) : IQuery<ApiResponse<List<CourseSummaryDto>>>;

public sealed class GetCoursesQueryHandler(
    IUnitOfWork uow,
    ICurrentUserService currentUser
) : IQueryHandler<GetCoursesQuery, ApiResponse<List<CourseSummaryDto>>>
{
    public async ValueTask<ApiResponse<List<CourseSummaryDto>>> Handle(
        GetCoursesQuery query, CancellationToken ct)
    {
        var isAdmin = currentUser.Role is "SuperAdmin" or "DepartmentAdmin";
        if (!isAdmin)
            return ApiResponse<List<CourseSummaryDto>>.Fail("Forbidden.");

        IEnumerable<Course> courses;
        if (query.TeacherId.HasValue)
            courses = await uow.Courses.GetByTeacherAsync(query.TeacherId.Value, ct);
        else if (query.StudentId.HasValue)
            courses = await uow.Courses.GetByStudentAsync(query.StudentId.Value, ct);
        else
            courses = await uow.Courses.GetAllAsync(ct);

        var dtos = new List<CourseSummaryDto>();
        foreach (var course in courses)
        {
            var teacher = await uow.Users.GetWithProfileAsync(course.TeacherId, ct);
            var members = await uow.CourseMembers.FindAsync(
                m => m.CourseId == course.Id && m.IsActive, ct);

            dtos.Add(course.ToSummaryDto(
                teacher?.Profile?.FullName ?? teacher?.Email ?? "Unknown",
                teacher?.Profile?.ProfilePhotoUrl,
                members.Count()));
        }

        return ApiResponse<List<CourseSummaryDto>>.Ok(dtos);
    }
}
