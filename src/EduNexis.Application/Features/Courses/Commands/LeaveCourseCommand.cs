namespace EduNexis.Application.Features.Courses.Commands;

public record LeaveCourseCommand(Guid CourseId) : ICommand<ApiResponse>;

public sealed class LeaveCourseCommandHandler(
    IUnitOfWork uow,
    ICurrentUserService currentUser
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

        return ApiResponse.Ok("Left the course successfully.");
    }
}
