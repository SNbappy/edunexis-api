using EduNexis.Application.DTOs;

using EduNexis.Application.Abstractions;

namespace EduNexis.Application.Features.Courses.Queries;

public record JoinRequestDto(
    Guid   Id,
    Guid   CourseId,
    Guid   StudentUserId,
    string StudentName,
    string StudentEmail,
    string? StudentIdNumber,
    string? ProfilePhotoUrl,
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

        bool isTeacher = await CourseAccess.IsTeacherAsync(uow, course, query.RequesterId, ct);
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
                Id:              r.Id,
                CourseId:        r.CourseId,
                StudentUserId:   r.StudentId,
                StudentName:     student?.Profile?.FullName ?? "Unknown student",
                StudentEmail:    student?.Email ?? "",
                StudentIdNumber: student?.Profile?.StudentId,
                ProfilePhotoUrl: student?.Profile?.ProfilePhotoUrl,
                Status:          r.Status.ToString(),
                CreatedAt:       r.CreatedAt));
        }

        return ApiResponse<List<JoinRequestDto>>.Ok(dtos);
    }
}
