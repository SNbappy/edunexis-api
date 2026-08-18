using EduNexis.Application.Abstractions;

namespace EduNexis.Application.Features.Courses.Queries;

public record CourseTeacherDto(
    Guid UserId,
    string FullName,
    string Email,
    string? ProfilePhotoUrl,
    string? Designation,
    /// <summary>True for the one owner; false for invited colleagues.</summary>
    bool IsOwner,
    DateTime AddedAt
);

public record CourseInvitationDto(
    Guid Id,
    Guid CourseId,
    string CourseTitle,
    string CourseCode,
    string InvitedByName,
    string? Message,
    string Status,
    DateTime CreatedAt
);

// ── Teachers on a course ─────────────────────────────────────────────

public record GetCourseTeachersQuery(Guid CourseId)
    : IQuery<ApiResponse<List<CourseTeacherDto>>>;

public sealed class GetCourseTeachersQueryHandler(
    IUnitOfWork uow
) : IQueryHandler<GetCourseTeachersQuery, ApiResponse<List<CourseTeacherDto>>>
{
    public async ValueTask<ApiResponse<List<CourseTeacherDto>>> Handle(
        GetCourseTeachersQuery query, CancellationToken ct)
    {
        var course = await uow.Courses.GetByIdAsync(query.CourseId, ct);
        if (course is null)
            return ApiResponse<List<CourseTeacherDto>>.Fail("Course not found.");

        var result = new List<CourseTeacherDto>();

        var owner = await uow.Users.GetWithProfileAsync(course.TeacherId, ct);
        if (owner is not null)
        {
            result.Add(new CourseTeacherDto(
                owner.Id,
                owner.Profile?.FullName ?? owner.Email,
                owner.Email,
                owner.Profile?.ProfilePhotoUrl,
                owner.Profile?.Designation,
                IsOwner: true,
                course.CreatedAt));
        }

        var co = (await uow.GetRepository<CourseTeacher>()
                .FindAsync(t => t.CourseId == query.CourseId, ct))
            .OrderBy(t => t.AddedAt)
            .ToList();

        foreach (var t in co)
        {
            var user = await uow.Users.GetWithProfileAsync(t.UserId, ct);
            if (user is null) continue;
            result.Add(new CourseTeacherDto(
                user.Id,
                user.Profile?.FullName ?? user.Email,
                user.Email,
                user.Profile?.ProfilePhotoUrl,
                user.Profile?.Designation,
                IsOwner: false,
                t.AddedAt));
        }

        return ApiResponse<List<CourseTeacherDto>>.Ok(result);
    }
}

// ── Pending invitations for a course (teacher view) ──────────────────

public record GetCourseInvitationsQuery(Guid CourseId)
    : IQuery<ApiResponse<List<CourseInvitationDto>>>;

public sealed class GetCourseInvitationsQueryHandler(
    IUnitOfWork uow
) : IQueryHandler<GetCourseInvitationsQuery, ApiResponse<List<CourseInvitationDto>>>
{
    public async ValueTask<ApiResponse<List<CourseInvitationDto>>> Handle(
        GetCourseInvitationsQuery query, CancellationToken ct)
    {
        var course = await uow.Courses.GetByIdAsync(query.CourseId, ct);
        if (course is null)
            return ApiResponse<List<CourseInvitationDto>>.Fail("Course not found.");

        var pending = (await uow.GetRepository<CourseInvitation>()
                .FindAsync(i =>
                    i.CourseId == query.CourseId &&
                    i.Status == CourseInvitationStatus.Pending, ct))
            .OrderByDescending(i => i.CreatedAt)
            .ToList();

        var dtos = new List<CourseInvitationDto>();
        foreach (var i in pending)
        {
            // Named by the person invited, since this list answers "who have we
            // asked" rather than "who asked".
            var invitee = await uow.Users.GetWithProfileAsync(i.InvitedUserId, ct);
            dtos.Add(new CourseInvitationDto(
                i.Id, course.Id, course.Title, course.CourseCode,
                invitee?.Profile?.FullName ?? invitee?.Email ?? "Unknown",
                i.Message, i.Status.ToString(), i.CreatedAt));
        }

        return ApiResponse<List<CourseInvitationDto>>.Ok(dtos);
    }
}

// ── My invitations (invitee view) ────────────────────────────────────

public record GetMyCourseInvitationsQuery(Guid UserId)
    : IQuery<ApiResponse<List<CourseInvitationDto>>>;

public sealed class GetMyCourseInvitationsQueryHandler(
    IUnitOfWork uow
) : IQueryHandler<GetMyCourseInvitationsQuery, ApiResponse<List<CourseInvitationDto>>>
{
    public async ValueTask<ApiResponse<List<CourseInvitationDto>>> Handle(
        GetMyCourseInvitationsQuery query, CancellationToken ct)
    {
        var pending = (await uow.GetRepository<CourseInvitation>()
                .FindAsync(i =>
                    i.InvitedUserId == query.UserId &&
                    i.Status == CourseInvitationStatus.Pending, ct))
            .OrderByDescending(i => i.CreatedAt)
            .ToList();

        var dtos = new List<CourseInvitationDto>();
        foreach (var i in pending)
        {
            var course = await uow.Courses.GetByIdAsync(i.CourseId, ct);
            // An invitation to a course the owner has since deleted is noise.
            if (course is null || course.IsDeletedByOwner || course.IsDeleted) continue;

            var inviter = await uow.Users.GetWithProfileAsync(i.InvitedById, ct);
            dtos.Add(new CourseInvitationDto(
                i.Id, course.Id, course.Title, course.CourseCode,
                inviter?.Profile?.FullName ?? "A colleague",
                i.Message, i.Status.ToString(), i.CreatedAt));
        }

        return ApiResponse<List<CourseInvitationDto>>.Ok(dtos);
    }
}
