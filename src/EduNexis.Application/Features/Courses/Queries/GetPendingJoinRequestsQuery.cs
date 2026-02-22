using EduNexis.Application.DTOs;

namespace EduNexis.Application.Features.Courses.Queries;

public record JoinRequestDto(
    Guid Id,
    Guid CourseId,
    Guid StudentId,
    string StudentName,
    string StudentEmail,
    string Status,
    DateTime CreatedAt
);

public record GetPendingJoinRequestsQuery(
    Guid CourseId,
    Guid RequesterId
) : IQuery<ApiResponse<List<JoinRequestDto>>>;

public sealed class GetPendingJoinRequestsQueryHandler(
    IUnitOfWork uow
) : IQueryHandler<GetPendingJoinRequestsQuery, ApiResponse<List<JoinRequestDto>>>
{
    public async ValueTask<ApiResponse<List<JoinRequestDto>>> Handle(
        GetPendingJoinRequestsQuery query, CancellationToken ct)
    {
        var course = await uow.Courses.GetByIdAsync(query.CourseId, ct)
            ?? throw new NotFoundException("Course", query.CourseId);

        bool isTeacher = course.TeacherId == query.RequesterId;
        var reviewer = await uow.CourseMembers.GetMemberAsync(course.Id, query.RequesterId, ct);
        bool isCR = reviewer?.IsCR ?? false;

        if (!isTeacher && !isCR)
            throw new UnauthorizedException("Only teacher or CR can view join requests.");

        var requests = await uow.JoinRequests.GetPendingByCourseAsync(query.CourseId, ct);

        var dtos = new List<JoinRequestDto>();
        foreach (var r in requests)
        {
            var student = await uow.Users.GetWithProfileAsync(r.StudentId, ct);
            dtos.Add(new JoinRequestDto(
                r.Id, r.CourseId, r.StudentId,
                student?.Profile?.FullName ?? "Unknown",
                student?.Email ?? "Unknown",
                r.Status.ToString(), r.CreatedAt));
        }

        return ApiResponse<List<JoinRequestDto>>.Ok(dtos);
    }
}
