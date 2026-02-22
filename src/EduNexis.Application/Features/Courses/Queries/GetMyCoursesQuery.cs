using EduNexis.Application.DTOs;

namespace EduNexis.Application.Features.Courses.Queries;

public record GetMyCoursesQuery(
    Guid UserId,
    UserRole Role
) : IQuery<ApiResponse<List<CourseSummaryDto>>>;

public sealed class GetMyCoursesQueryHandler(
    IUnitOfWork uow
) : IQueryHandler<GetMyCoursesQuery, ApiResponse<List<CourseSummaryDto>>>
{
    public async ValueTask<ApiResponse<List<CourseSummaryDto>>> Handle(
        GetMyCoursesQuery query, CancellationToken ct)
    {
        IEnumerable<Course> courses = query.Role == UserRole.Teacher
            ? await uow.Courses.GetByTeacherAsync(query.UserId, ct)
            : await uow.Courses.GetByStudentAsync(query.UserId, ct);

        var dtos = new List<CourseSummaryDto>();

        foreach (var course in courses)
        {
            var teacher = await uow.Users.GetWithProfileAsync(course.TeacherId, ct);
            dtos.Add(new CourseSummaryDto(
                course.Id, course.Title, course.CourseCode,
                course.Department, course.AcademicSession,
                course.CoverImageUrl,
                teacher?.Profile?.FullName ?? teacher?.Email ?? "Unknown",
                course.IsArchived));
        }

        return ApiResponse<List<CourseSummaryDto>>.Ok(dtos);
    }
}
