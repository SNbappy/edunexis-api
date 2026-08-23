using EduNexis.Application.Abstractions;
using EduNexis.Application.Features.Notifications.Commands;

namespace EduNexis.Application.Features.Courses.Commands;

public record RemoveCourseMemberCommand(Guid CourseId, Guid StudentId) : ICommand<ApiResponse>, ICourseScopedWrite
{
    public ValueTask<Guid?> ResolveCourseIdAsync(IUnitOfWork uow, CancellationToken ct)
        => ValueTask.FromResult<Guid?>(CourseId);
}

public sealed class RemoveCourseMemberCommandHandler(
    IUnitOfWork uow,
    ICurrentUserService currentUser,
    ISender sender
) : ICommandHandler<RemoveCourseMemberCommand, ApiResponse>
{
    public async ValueTask<ApiResponse> Handle(
        RemoveCourseMemberCommand cmd, CancellationToken ct)
    {
        var requesterId = Guid.Parse(currentUser.UserId);

        var course = await uow.Courses.GetByIdAsync(cmd.CourseId, ct);
        if (course is null)
            return ApiResponse.Fail("Course not found.");

        var isTeacher = await CourseAccess.IsTeacherAsync(uow, course, requesterId, ct);
        var requesterMembership = await uow.CourseMembers.GetMemberAsync(cmd.CourseId, requesterId, ct);
        var isCR = requesterMembership?.IsCR == true && requesterMembership.IsActive;
        var isPlatformAdmin = currentUser.Role == "SuperAdmin";

        if (!isTeacher && !isCR && !isPlatformAdmin)
            return ApiResponse.Fail("You are not authorized to remove members from this course.");

        if (isCR && !isTeacher && !isPlatformAdmin)
        {
            var isTargetTeacher = await CourseAccess.IsTeacherAsync(uow, course, cmd.StudentId, ct);
            if (isTargetTeacher)
                return ApiResponse.Fail("Class representatives cannot remove teachers from the course.");

            var targetMembership = await uow.CourseMembers.GetMemberAsync(cmd.CourseId, cmd.StudentId, ct);
            if (targetMembership?.IsCR == true)
                return ApiResponse.Fail("Class representatives cannot remove another class representative.");
        }

        var member = await uow.CourseMembers.GetMemberAsync(cmd.CourseId, cmd.StudentId, ct);
        if (member is null || !member.IsActive)
            return ApiResponse.Fail("Student is not a member of this course.");

        member.Remove();
        uow.CourseMembers.Update(member);
        await uow.SaveChangesAsync(ct);

        await sender.Send(new SendNotificationCommand(
            UserId: cmd.StudentId,
            Title: "Removed from Course",
            Body: $"You have been removed from {course.Title}.",
            Type: NotificationType.General,
            RedirectUrl: null
        ), ct);

        return ApiResponse.Ok("Student removed from course.");
    }
}
