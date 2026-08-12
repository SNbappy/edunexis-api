using EduNexis.Application.Abstractions;
using EduNexis.Application.DTOs;
using EduNexis.Application.Extensions;
using EduNexis.Domain.Enums;

namespace EduNexis.Application.Features.Courses.Queries;

public record GetCourseQuery(Guid Id) : IQuery<ApiResponse<CourseDto>>;

public sealed class GetCourseQueryHandler(
    IUnitOfWork uow,
    ICurrentUserService currentUser
) : IQueryHandler<GetCourseQuery, ApiResponse<CourseDto>>
{
    public async ValueTask<ApiResponse<CourseDto>> Handle(
        GetCourseQuery query, CancellationToken ct)
    {
        var course = await uow.Courses.GetByIdAsync(query.Id, ct);
        if (course is null)
            return ApiResponse<CourseDto>.Fail("Course not found.");

        var viewerId    = Guid.Parse(currentUser.UserId);
        var isAdmin     = currentUser.Role is "SuperAdmin" or "DepartmentAdmin";
        var isOwner     = course.TeacherId == viewerId;
        var membership  = await uow.CourseMembers.GetMemberAsync(course.Id, viewerId, ct);
        var isMember    = membership is not null && membership.IsActive;

        // Access control — owner, admin, or active member only
        if (!isOwner && !isAdmin && !isMember)
            return ApiResponse<CourseDto>.Fail("You don't have access to this course.");

        var viewerRole = isOwner ? "Owner" : "Member";

        var teacher = await uow.Users.GetWithProfileAsync(course.TeacherId, ct);
        var members = await uow.CourseMembers.FindAsync(
            m => m.CourseId == course.Id && m.IsActive, ct);

        return ApiResponse<CourseDto>.Ok(course.ToDto(
            teacher?.Profile?.FullName ?? teacher?.Email ?? "Unknown",
            teacher?.Profile?.ProfilePhotoUrl,
            members.Count(),
            viewerRole));
    }
}
