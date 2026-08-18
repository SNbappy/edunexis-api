using EduNexis.Application.Abstractions;
using EduNexis.Application.Features.Notifications.Commands;
using EduNexis.Domain.Entities;

namespace EduNexis.Application.Features.Courses.Commands;

public record ReviewJoinRequestCommand(
    Guid CourseId,
    Guid RequestId,
    bool Approve
) : ICommand<ApiResponse>, ICourseScopedWrite
{
    public ValueTask<Guid?> ResolveCourseIdAsync(IUnitOfWork uow, CancellationToken ct)
        => ValueTask.FromResult<Guid?>(CourseId);
}

public sealed class ReviewJoinRequestCommandValidator : AbstractValidator<ReviewJoinRequestCommand>
{
    public ReviewJoinRequestCommandValidator()
    {
        RuleFor(x => x.CourseId).NotEmpty();
        RuleFor(x => x.RequestId).NotEmpty();
    }
}

public sealed class ReviewJoinRequestCommandHandler(
    IUnitOfWork uow,
    ICurrentUserService currentUser,
    ISender sender
) : ICommandHandler<ReviewJoinRequestCommand, ApiResponse>
{
    public async ValueTask<ApiResponse> Handle(
        ReviewJoinRequestCommand cmd, CancellationToken ct)
    {
        var reviewerId = Guid.Parse(currentUser.UserId);

        var course = await uow.Courses.GetByIdAsync(cmd.CourseId, ct);
        if (course is null)
            return ApiResponse.Fail("Course not found.");

        /* Who may admit a student.
           Two things were wrong here. "Admin" is not a role this app has — the
           only admin role is SuperAdmin — so that arm could never be true and a
           platform admin was refused. And a class representative could already
           *see* pending requests (GetPendingJoinRequestsQuery honours IsCR) but
           was refused when they tried to act on one, which is the single thing
           the role exists to do. */
        var reviewerMembership = await uow.CourseMembers.GetMemberAsync(cmd.CourseId, reviewerId, ct);
        var isCR = reviewerMembership?.IsCR == true && reviewerMembership.IsActive;
        var isOwner = course.TeacherId == reviewerId;
        var isPlatformAdmin = currentUser.Role == "SuperAdmin";

        if (!isOwner && !isCR && !isPlatformAdmin)
            return ApiResponse.Fail("You are not authorized to review join requests for this course.");

        var request = await uow.JoinRequests.GetByIdAsync(cmd.RequestId, ct);
        if (request is null || request.CourseId != cmd.CourseId)
            return ApiResponse.Fail("Join request not found.");

        if (request.Status != JoinRequestStatus.Pending)
            return ApiResponse.Fail("This request has already been reviewed.");

        if (cmd.Approve)
        {
            request.Approve(reviewerId);

            var existing = await uow.CourseMembers.GetMemberAsync(cmd.CourseId, request.StudentId, ct);
            if (existing is not null)
            {
                existing.Reactivate();
                uow.CourseMembers.Update(existing);
            }
            else
            {
                await uow.CourseMembers.AddAsync(
                    CourseMember.Create(cmd.CourseId, request.StudentId), ct);
            }
        }
        else
        {
            request.Reject(reviewerId);
        }

        uow.JoinRequests.Update(request);
        await uow.SaveChangesAsync(ct);

        // Notify student of decision
        var (title, body, type) = cmd.Approve
            // Was "Join Request Approved ??" - an emoji that an ANSI save had
            // already flattened to two literal question marks, which then went
            // into the database and showed as "??" in the student's feed.
            ? ("Join Request Approved",
               $"Your request to join {course.Title} has been approved. Welcome!",
               NotificationType.CourseJoinApproved)
            : ("Join Request Rejected",
               $"Your request to join {course.Title} was not approved.",
               NotificationType.CourseJoinRejected);

        await sender.Send(new SendNotificationCommand(
            UserId: request.StudentId,
            Title: title,
            Body: body,
            Type: type,
            RedirectUrl: cmd.Approve ? $"/courses/{course.Id}/stream" : null
        ), ct);

        return ApiResponse.Ok(cmd.Approve ? "Join request approved." : "Join request rejected.");
    }
}
