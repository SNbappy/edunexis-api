using EduNexis.Application.DTOs;

namespace EduNexis.Application.Features.Courses.Queries;

/// <summary>
/// Used by the Join Course flow — student enters an 8-char code, we resolve
/// it to a minimal course identity so the UI can show a confirmation preview.
/// </summary>
public record GetCourseByCodeQuery(string Code) : IQuery<ApiResponse<CourseByCodeDto>>;

/// <summary>
/// Compact course identity — just enough for a "you're about to join" preview.
/// Does NOT include sensitive details, members, or teacher contact info.
/// </summary>
public record CourseByCodeDto(
    Guid Id,
    string Title,
    string CourseCode,
    string Department,
    string Semester,
    string CourseType,
    string TeacherName,
    string? TeacherProfilePhotoUrl,
    int MemberCount
);

public sealed class GetCourseByCodeQueryHandler(
    IUnitOfWork uow
) : IQueryHandler<GetCourseByCodeQuery, ApiResponse<CourseByCodeDto>>
{
    public async ValueTask<ApiResponse<CourseByCodeDto>> Handle(
        GetCourseByCodeQuery query, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(query.Code))
            return ApiResponse<CourseByCodeDto>.Fail("Joining code is required.");

        var normalized = query.Code.Trim().ToUpperInvariant();
        var course = await uow.Courses.GetByJoiningCodeAsync(normalized, ct);

        // Deleted as well as archived: a deleted course's joining code was still
        // resolvable, so an old code could be used to request a place in a
        // course that no longer exists. `IsDeletedByOwner` is the teacher's
        // recycle bin; `IsDeleted` is an admin purge.
        if (course is null || course.IsArchived || course.IsDeletedByOwner || course.IsDeleted)
            return ApiResponse<CourseByCodeDto>.Fail("No course found with that code.");

        var members = await uow.CourseMembers.FindAsync(
            m => m.CourseId == course.Id && m.IsActive, ct);

        return ApiResponse<CourseByCodeDto>.Ok(new CourseByCodeDto(
            course.Id,
            course.Title,
            course.CourseCode,
            course.Department,
            course.Semester,
            course.CourseType.ToString(),
            course.Teacher?.Profile?.FullName ?? course.Teacher?.Email ?? "Unknown",
            course.Teacher?.Profile?.ProfilePhotoUrl,
            members.Count()
        ));
    }
}
