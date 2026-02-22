using EduNexis.Application.DTOs;

namespace EduNexis.Application.Features.Courses.Queries;

public record GetCourseQuery(Guid CourseId) : IQuery<ApiResponse<CourseDto>>;

public sealed class GetCourseQueryHandler(
    IUnitOfWork uow
) : IQueryHandler<GetCourseQuery, ApiResponse<CourseDto>>
{
    public async ValueTask<ApiResponse<CourseDto>> Handle(
        GetCourseQuery query, CancellationToken ct)
    {
        var course = await uow.Courses.GetWithMembersAsync(query.CourseId, ct)
            ?? throw new NotFoundException("Course", query.CourseId);

        var teacher = await uow.Users.GetWithProfileAsync(course.TeacherId, ct)
            ?? throw new NotFoundException("User", course.TeacherId);

        return ApiResponse<CourseDto>.Ok(new CourseDto(
            course.Id, course.Title, course.CourseCode, course.CreditHours,
            course.Department, course.AcademicSession, course.Semester,
            course.Section, course.CourseType.ToString(), course.Description,
            course.CoverImageUrl, course.JoiningCode, course.TeacherId,
            teacher.Profile?.FullName ?? teacher.Email, course.IsArchived,
            course.Members.Count(m => m.IsActive), course.CreatedAt));
    }
}
