using EduNexis.Domain.Entities;

namespace EduNexis.Application.Features.Courses.Commands;

public record RequestJoinCourseCommand(Guid CourseId) : ICommand<ApiResponse>;

public sealed class RequestJoinCourseCommandValidator : AbstractValidator<RequestJoinCourseCommand>
{
    public RequestJoinCourseCommandValidator()
    {
        RuleFor(x => x.CourseId).NotEmpty();
    }
}

public sealed class RequestJoinCourseCommandHandler(
    IUnitOfWork uow,
    ICurrentUserService currentUser
) : ICommandHandler<RequestJoinCourseCommand, ApiResponse>
{
    public async ValueTask<ApiResponse> Handle(
        RequestJoinCourseCommand cmd, CancellationToken ct)
    {
        var userId = Guid.Parse(currentUser.UserId);

        var course = await uow.Courses.GetByIdAsync(cmd.CourseId, ct);
        if (course is null)
            return ApiResponse.Fail("Course not found.");

        if (course.IsArchived)
            return ApiResponse.Fail("This course is no longer active.");

        var existingMember = await uow.CourseMembers.GetMemberAsync(cmd.CourseId, userId, ct);
        if (existingMember is not null && existingMember.IsActive)
            return ApiResponse.Fail("You are already a member of this course.");

        var existingRequest = await uow.JoinRequests.GetPendingAsync(cmd.CourseId, userId, ct);
        if (existingRequest is not null)
            return ApiResponse.Fail("You already have a pending join request for this course.");

        await uow.JoinRequests.AddAsync(JoinRequest.Create(cmd.CourseId, userId), ct);
        await uow.SaveChangesAsync(ct);

        return ApiResponse.Ok("Join request sent successfully.");
    }
}
