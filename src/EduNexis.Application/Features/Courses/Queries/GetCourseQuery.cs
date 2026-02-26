using EduNexis.Application.DTOs;
using EduNexis.Application.Extensions;

namespace EduNexis.Application.Features.Courses.Queries;

public record GetCourseQuery(Guid Id) : IQuery<ApiResponse<CourseDto>>;

public sealed class GetCourseQueryHandler(
    IUnitOfWork uow
) : IQueryHandler<GetCourseQuery, ApiResponse<CourseDto>>
{
    public async ValueTask<ApiResponse<CourseDto>> Handle(
        GetCourseQuery query, CancellationToken ct)
    {
        var course = await uow.Courses.GetByIdAsync(query.Id, ct);
        return course is null
            ? ApiResponse<CourseDto>.Fail("Course not found.")
            : ApiResponse<CourseDto>.Ok(course.ToDto());
    }
}
