using EduNexis.Application.DTOs;
using EduNexis.Application.Extensions;

namespace EduNexis.Application.Features.Courses.Queries;

public record GetCourseMembersQuery(Guid CourseId)
    : IQuery<ApiResponse<List<CourseMemberDto>>>;

public sealed class GetCourseMembersQueryHandler(IUnitOfWork uow)
    : IQueryHandler<GetCourseMembersQuery, ApiResponse<List<CourseMemberDto>>>
{
    public async ValueTask<ApiResponse<List<CourseMemberDto>>> Handle(
        GetCourseMembersQuery query, CancellationToken ct)
    {
        var course = await uow.Courses.GetByIdAsync(query.CourseId, ct);
        if (course is null)
            return ApiResponse<List<CourseMemberDto>>.Fail("Course not found.");

        var members = await uow.CourseMembers.GetByCourseAsync(query.CourseId, ct);
        var dtos = members
            .Where(m => m.IsActive)
            .OrderBy(m => m.User?.Profile?.FullName)
            .Select(m => m.ToMemberDto())
            .ToList();

        return ApiResponse<List<CourseMemberDto>>.Ok(dtos, $"{dtos.Count} members found.");
    }
}
