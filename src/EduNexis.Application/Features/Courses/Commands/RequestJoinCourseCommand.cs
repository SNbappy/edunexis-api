using EduNexis.Application.Abstractions;
﻿using EduNexis.Application.Features.Notifications.Commands;
using EduNexis.Domain.Entities;
using EduNexis.Domain.Enums;

namespace EduNexis.Application.Features.Courses.Commands;

public record RequestJoinCourseCommand(Guid CourseId, string JoiningCode) : ICommand<ApiResponse>, IArchiveExempt
{
    public string ArchiveExemptionReason => "Already refuses archived courses with its own message.";
}

public sealed class RequestJoinCourseCommandValidator : AbstractValidator<RequestJoinCourseCommand>
{
    public RequestJoinCourseCommandValidator()
    {
        RuleFor(x => x.CourseId).NotEmpty();
        RuleFor(x => x.JoiningCode).NotEmpty().WithMessage("Joining code is required.");
    }
}

public sealed class RequestJoinCourseCommandHandler(
    IUnitOfWork uow,
    ICurrentUserService currentUser,
    ISender sender
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

        if (!string.Equals(course.JoiningCode, cmd.JoiningCode, StringComparison.OrdinalIgnoreCase))
            return ApiResponse.Fail("Invalid joining code.");

        var existingMember = await uow.CourseMembers.GetMemberAsync(cmd.CourseId, userId, ct);
        if (existingMember is not null && existingMember.IsActive)
            return ApiResponse.Fail("You are already a member of this course.");

        var existingRequest = await uow.JoinRequests.GetPendingAsync(cmd.CourseId, userId, ct);
        if (existingRequest is not null)
            return ApiResponse.Fail("You already have a pending join request for this course.");

        // When re-requesting a course we were previously rejected from, auto-dismiss
        // the old Rejected row(s). Otherwise the student would see a stale
        // "Rejected" card even after being approved on the new request.
        var previousRequests = await uow.JoinRequests.FindAsync(
            r => r.CourseId == cmd.CourseId && r.StudentId == userId, ct);
        foreach (var prev in previousRequests)
        {
            if (prev.Status == JoinRequestStatus.Rejected && !prev.IsDismissedByStudent)
            {
                prev.DismissByStudent();
                uow.JoinRequests.Update(prev);
            }
        }

        await uow.JoinRequests.AddAsync(JoinRequest.Create(cmd.CourseId, userId), ct);
        await uow.SaveChangesAsync(ct);

        var student = await uow.Users.GetWithProfileAsync(userId, ct);
        var studentName = student?.Profile?.FullName ?? "A student";

        var teacherIds = await CourseAccess.TeacherIdsAsync(uow, course, ct);
        var crMembers = await uow.CourseMembers.FindAsync(
            m => m.CourseId == course.Id && m.IsActive && m.IsCR, ct);
        var recipientIds = teacherIds.Concat(crMembers.Select(m => m.UserId)).ToHashSet();

        foreach (var recipientId in recipientIds)
        {
            await sender.Send(new SendNotificationCommand(
                UserId: recipientId,
                Title: "New Join Request",
                Body: $"{studentName} has requested to join {course.Title}.",
                Type: NotificationType.JoinRequestReceived,
                RedirectUrl: $"/courses/{course.Id}/members?view=requests"
            ), ct);
        }

        return ApiResponse.Ok("Join request sent successfully.");
    }
}


