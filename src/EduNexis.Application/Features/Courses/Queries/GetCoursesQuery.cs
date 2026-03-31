using EduNexis.Application.DTOs;
using EduNexis.Application.Extensions;

namespace EduNexis.Application.Features.Courses.Queries;

public record GetCoursesQuery(
    Guid? TeacherId = null,
    Guid? StudentId = null
) : IQuery<ApiResponse<List<CourseDto>>>;

public sealed class GetCoursesQueryHandler(
    IUnitOfWork uow
) : IQueryHandler<GetCoursesQuery, ApiResponse<List<CourseDto>>>
{
    public async ValueTask<ApiResponse<List<CourseDto>>> Handle(
        GetCoursesQuery query, CancellationToken ct)
    {
        IEnumerable<Course> courses;

        if (query.TeacherId.HasValue)
            courses = await uow.Courses.GetByTeacherAsync(query.TeacherId.Value, ct);
        else if (query.StudentId.HasValue)
            courses = await uow.Courses.GetByStudentAsync(query.StudentId.Value, ct);
        else
            courses = await uow.Courses.GetAllAsync(ct);

        var dtos = new List<CourseDto>();
        foreach (var course in courses)
        {
            var teacher = await uow.Users.GetWithProfileAsync(course.TeacherId, ct);
            dtos.Add(course.ToDto(
                teacher?.Profile?.FullName ?? teacher?.Email ?? "Unknown",
                teacher?.Profile?.ProfilePhotoUrl));
        }

        return ApiResponse<List<CourseDto>>.Ok(dtos);
    }
}
