namespace EduNexis.Application.Features.Courses.Commands;

public record ReviewJoinRequestCommand(
    Guid RequestId,
    Guid ReviewerId,
    bool Approve
) : ICommand<ApiResponse>;

public sealed class ReviewJoinRequestCommandHandler(
    IUnitOfWork uow
) : ICommandHandler<ReviewJoinRequestCommand, ApiResponse>
{
    public async ValueTask<ApiResponse> Handle(
        ReviewJoinRequestCommand command, CancellationToken ct)
    {
        var request = await uow.JoinRequests.GetByIdAsync(command.RequestId, ct)
            ?? throw new NotFoundException("JoinRequest", command.RequestId);

        var course = await uow.Courses.GetByIdAsync(request.CourseId, ct)
            ?? throw new NotFoundException("Course", request.CourseId);

        // Only teacher or CR can review
        var reviewer = await uow.CourseMembers.GetMemberAsync(
            course.Id, command.ReviewerId, ct);

        bool isTeacher = course.TeacherId == command.ReviewerId;
        bool isCR = reviewer?.IsCR ?? false;

        if (!isTeacher && !isCR)
            throw new UnauthorizedException("Only the teacher or CR can review join requests.");

        if (command.Approve)
        {
            request.Approve(command.ReviewerId);
            var member = CourseMember.Create(request.CourseId, request.StudentId);
            await uow.CourseMembers.AddAsync(member, ct);
        }
        else
        {
            request.Reject(command.ReviewerId);
        }

        uow.JoinRequests.Update(request);
        await uow.SaveChangesAsync(ct);

        return ApiResponse.Ok(command.Approve
            ? "Join request approved."
            : "Join request rejected.");
    }
}
