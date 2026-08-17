using EduNexis.Application.Features.Notifications.Commands;
using EduNexis.Application.Abstractions;
namespace EduNexis.Application.Features.Courses.Commands;

public record LeaveCourseCommand(Guid CourseId) : ICommand<ApiResponse>, IArchiveExempt
{
    public string ArchiveExemptionReason => "A student may always leave, including an archived course.";
}

public sealed class LeaveCourseCommandHandler(
    IUnitOfWork uow,
    ICurrentUserService currentUser,
    ISender sender
) : ICommandHandler<LeaveCourseCommand, ApiResponse>
{
    public async ValueTask<ApiResponse> Handle(
        LeaveCourseCommand cmd, CancellationToken ct)
    {
        var userId = Guid.Parse(currentUser.UserId);

        var member = await uow.CourseMembers.GetMemberAsync(cmd.CourseId, userId, ct);
        if (member is null || !member.IsActive)
            return ApiResponse.Fail("You are not enrolled in this course.");

        member.Remove();
        uow.CourseMembers.Update(member);
        await uow.SaveChangesAsync(ct);

        // Tell the teacher. A roster that silently shrinks is how a student
        // ends up missing from a gradebook with nobody knowing why.
        var course = await uow.Courses.GetByIdAsync(cmd.CourseId, ct);
        if (course is not null)
        {
            var profile = await uow.UserProfiles
                .FirstOrDefaultAsync(p => p.UserId == userId, ct);

            await sender.Send(new SendNotificationCommand(
                UserId: course.TeacherId,
                Title: $"A student left {course.Title}",
                Body: $"{profile?.FullName ?? "A student"} is no longer enrolled.",
                Type: NotificationType.MemberLeft,
                RedirectUrl: $"/courses/{course.Id}/members"
            ), ct);
        }

        return ApiResponse.Ok("Left the course successfully.");
    }
}
