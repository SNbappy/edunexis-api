using EduNexis.Application.Features.Notifications.Commands;
using EduNexis.Domain.Entities;

namespace EduNexis.Application.Features.Courses.Commands;

public record RequestJoinByCodeCommand(string JoiningCode) : ICommand<ApiResponse>;

public sealed class RequestJoinByCodeCommandValidator : AbstractValidator<RequestJoinByCodeCommand>
{
    public RequestJoinByCodeCommandValidator()
    {
        RuleFor(x => x.JoiningCode).NotEmpty().WithMessage("Joining code is required.");
    }
}

public sealed class RequestJoinByCodeCommandHandler(
    IUnitOfWork uow,
    ICurrentUserService currentUser,
    ISender sender
) : ICommandHandler<RequestJoinByCodeCommand, ApiResponse>
{
    public async ValueTask<ApiResponse> Handle(
        RequestJoinByCodeCommand cmd, CancellationToken ct)
    {
        var userId = Guid.Parse(currentUser.UserId);

        var course = await uow.Courses.GetByJoiningCodeAsync(cmd.JoiningCode, ct);
        if (course is null)
            return ApiResponse.Fail("Invalid joining code.");

        if (course.IsArchived)
            return ApiResponse.Fail("This course is no longer active.");

        var existingMember = await uow.CourseMembers.GetMemberAsync(course.Id, userId, ct);
        if (existingMember is not null && existingMember.IsActive)
            return ApiResponse.Fail("You are already a member of this course.");

        var existingRequest = await uow.JoinRequests.GetPendingAsync(course.Id, userId, ct);
        if (existingRequest is not null)
            return ApiResponse.Fail("You already have a pending join request for this course.");

        await uow.JoinRequests.AddAsync(JoinRequest.Create(course.Id, userId), ct);
        await uow.SaveChangesAsync(ct);

        var student = await uow.Users.GetWithProfileAsync(userId, ct);
        var studentName = student?.Profile?.FullName ?? "A student";

        await sender.Send(new SendNotificationCommand(
            UserId: course.TeacherId,
            Title: "New Join Request",
            Body: $"{studentName} has requested to join {course.Title}.",
            Type: NotificationType.JoinRequestReceived,
            RedirectUrl: $"/courses/{course.Id}/members"
        ), ct);

        return ApiResponse.Ok("Join request sent successfully.");
    }
}
