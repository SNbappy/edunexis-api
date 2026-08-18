using EduNexis.Application.Abstractions;
using EduNexis.Application.Features.Notifications.Commands;

namespace EduNexis.Application.Features.Courses.Commands;

/*
 * Sharing a course with another teacher.
 *
 * Invite → the colleague accepts → a CourseTeacher row appears. Nobody is added
 * to a course without agreeing to it, so accepting is what actually grants
 * access; the invitation on its own grants nothing.
 */

// ── Invite ───────────────────────────────────────────────────────────

public record InviteTeacherCommand(
    Guid CourseId,
    Guid RequestedById,
    string Email,
    string? Message = null
) : ICommand<ApiResponse>, ICourseScopedWrite
{
    public ValueTask<Guid?> ResolveCourseIdAsync(IUnitOfWork uow, CancellationToken ct)
        => ValueTask.FromResult<Guid?>(CourseId);
}

public sealed class InviteTeacherCommandValidator : AbstractValidator<InviteTeacherCommand>
{
    public InviteTeacherCommandValidator()
    {
        RuleFor(x => x.Email).NotEmpty().EmailAddress();
        RuleFor(x => x.Message).MaximumLength(500);
    }
}

public sealed class InviteTeacherCommandHandler(
    IUnitOfWork uow,
    ISender sender
) : ICommandHandler<InviteTeacherCommand, ApiResponse>
{
    public async ValueTask<ApiResponse> Handle(InviteTeacherCommand cmd, CancellationToken ct)
    {
        var course = await uow.Courses.GetByIdAsync(cmd.CourseId, ct);
        if (course is null) return ApiResponse.Fail("Course not found.");

        // Any teacher on the course may bring in another. Restricting this to
        // the owner means a shared course stops being shareable the moment the
        // owner is on leave — which is exactly when help is needed.
        if (!await CourseAccess.IsTeacherAsync(uow, course, cmd.RequestedById, ct))
            return ApiResponse.Fail("Only a teacher on this course can invite a colleague.");

        var email = cmd.Email.Trim().ToLowerInvariant();
        var invitee = await uow.Users.FirstOrDefaultAsync(u => u.Email.ToLower() == email, ct);

        if (invitee is null)
            return ApiResponse.Fail("No account with that email. They need to register first.");

        if (invitee.Role != UserRole.Teacher && invitee.Role != UserRole.SuperAdmin)
            return ApiResponse.Fail("Only a teacher account can be invited to co-teach.");

        if (invitee.Id == course.TeacherId)
            return ApiResponse.Fail("They already own this course.");

        if (await CourseAccess.IsCoTeacherAsync(uow, course.Id, invitee.Id, ct))
            return ApiResponse.Fail("They are already teaching this course.");

        var pending = await uow.GetRepository<CourseInvitation>()
            .FirstOrDefaultAsync(i =>
                i.CourseId == course.Id &&
                i.InvitedUserId == invitee.Id &&
                i.Status == CourseInvitationStatus.Pending, ct);

        if (pending is not null)
            return ApiResponse.Fail("They already have a pending invitation to this course.");

        var invitation = CourseInvitation.Create(course.Id, invitee.Id, cmd.RequestedById, cmd.Message);
        await uow.GetRepository<CourseInvitation>().AddAsync(invitation, ct);
        await uow.SaveChangesAsync(ct);

        var inviter = await uow.Users.GetWithProfileAsync(cmd.RequestedById, ct);
        var inviterName = inviter?.Profile?.FullName ?? "A colleague";

        await sender.Send(new SendNotificationCommand(
            UserId: invitee.Id,
            Title: $"Invitation to co-teach {course.Title}",
            Body: $"{inviterName} has invited you to help run {course.CourseCode} — {course.Title}.",
            Type: NotificationType.General,
            RedirectUrl: "/courses?filter=invitations"
        ), ct);

        return ApiResponse.Ok("Invitation sent.");
    }
}

// ── Respond ──────────────────────────────────────────────────────────

public record RespondToCourseInvitationCommand(
    Guid InvitationId,
    Guid UserId,
    bool Accept
) : ICommand<ApiResponse>, IArchiveExempt
{
    /// <summary>
    /// Deliberately not course-scoped. The archive freeze resolves the course
    /// and blocks the write, which would trap somebody holding an invitation to
    /// a course that has since been archived: they could neither accept nor
    /// decline, and the invitation would sit in their list forever. Answering an
    /// invitation is about the invitation, not about the course's contents.
    /// </summary>
    public string ArchiveExemptionReason =>
        "Answering an invitation must stay possible even if the course is archived.";
}

public sealed class RespondToCourseInvitationCommandHandler(
    IUnitOfWork uow,
    ISender sender
) : ICommandHandler<RespondToCourseInvitationCommand, ApiResponse>
{
    public async ValueTask<ApiResponse> Handle(
        RespondToCourseInvitationCommand cmd, CancellationToken ct)
    {
        var invitation = await uow.GetRepository<CourseInvitation>()
            .GetByIdAsync(cmd.InvitationId, ct);

        // Only the person invited can answer, and only their own invitation —
        // otherwise an id guessed from somebody else's notification would do.
        if (invitation is null || invitation.InvitedUserId != cmd.UserId)
            return ApiResponse.Fail("Invitation not found.");

        if (invitation.Status != CourseInvitationStatus.Pending)
            return ApiResponse.Fail("This invitation has already been answered.");

        var course = await uow.Courses.GetByIdAsync(invitation.CourseId, ct);
        if (course is null || course.IsDeletedByOwner || course.IsDeleted)
            return ApiResponse.Fail("That course no longer exists.");

        if (!cmd.Accept)
        {
            invitation.Decline();
            uow.GetRepository<CourseInvitation>().Update(invitation);
            await uow.SaveChangesAsync(ct);
            return ApiResponse.Ok("Invitation declined.");
        }

        invitation.Accept();
        uow.GetRepository<CourseInvitation>().Update(invitation);

        // Guard against a double-accept from two tabs; the unique index would
        // otherwise surface as a raw database error.
        if (!await CourseAccess.IsCoTeacherAsync(uow, course.Id, cmd.UserId, ct))
        {
            await uow.GetRepository<CourseTeacher>().AddAsync(
                CourseTeacher.Create(course.Id, cmd.UserId, invitation.InvitedById), ct);
        }

        await uow.SaveChangesAsync(ct);

        var accepter = await uow.Users.GetWithProfileAsync(cmd.UserId, ct);
        var name = accepter?.Profile?.FullName ?? "A teacher";

        await sender.Send(new SendNotificationCommand(
            UserId: invitation.InvitedById,
            Title: $"{name} joined {course.Title}",
            Body: $"They accepted your invitation to co-teach {course.CourseCode}.",
            Type: NotificationType.General,
            RedirectUrl: $"/courses/{course.Id}/members"
        ), ct);

        return ApiResponse.Ok($"You are now teaching {course.Title}.");
    }
}

// ── Revoke ───────────────────────────────────────────────────────────

public record RevokeCourseInvitationCommand(
    Guid CourseId,
    Guid InvitationId,
    Guid RequestedById
) : ICommand<ApiResponse>, ICourseScopedWrite
{
    public ValueTask<Guid?> ResolveCourseIdAsync(IUnitOfWork uow, CancellationToken ct)
        => ValueTask.FromResult<Guid?>(CourseId);
}

public sealed class RevokeCourseInvitationCommandHandler(
    IUnitOfWork uow
) : ICommandHandler<RevokeCourseInvitationCommand, ApiResponse>
{
    public async ValueTask<ApiResponse> Handle(
        RevokeCourseInvitationCommand cmd, CancellationToken ct)
    {
        var course = await uow.Courses.GetByIdAsync(cmd.CourseId, ct);
        if (course is null) return ApiResponse.Fail("Course not found.");

        if (!await CourseAccess.IsTeacherAsync(uow, course, cmd.RequestedById, ct))
            return ApiResponse.Fail("You are not a teacher on this course.");

        var invitation = await uow.GetRepository<CourseInvitation>()
            .GetByIdAsync(cmd.InvitationId, ct);

        if (invitation is null || invitation.CourseId != cmd.CourseId)
            return ApiResponse.Fail("Invitation not found.");

        if (invitation.Status != CourseInvitationStatus.Pending)
            return ApiResponse.Fail("Only a pending invitation can be withdrawn.");

        invitation.Revoke();
        uow.GetRepository<CourseInvitation>().Update(invitation);
        await uow.SaveChangesAsync(ct);

        return ApiResponse.Ok("Invitation withdrawn.");
    }
}

// ── Remove a co-teacher ──────────────────────────────────────────────

public record RemoveCoTeacherCommand(
    Guid CourseId,
    Guid TeacherId,
    Guid RequestedById
) : ICommand<ApiResponse>, ICourseScopedWrite
{
    public ValueTask<Guid?> ResolveCourseIdAsync(IUnitOfWork uow, CancellationToken ct)
        => ValueTask.FromResult<Guid?>(CourseId);
}

public sealed class RemoveCoTeacherCommandHandler(
    IUnitOfWork uow,
    ISender sender
) : ICommandHandler<RemoveCoTeacherCommand, ApiResponse>
{
    public async ValueTask<ApiResponse> Handle(RemoveCoTeacherCommand cmd, CancellationToken ct)
    {
        var course = await uow.Courses.GetByIdAsync(cmd.CourseId, ct);
        if (course is null) return ApiResponse.Fail("Course not found.");

        // The owner can remove anybody; a co-teacher may only step down
        // themselves. Otherwise two colleagues could remove each other.
        var isOwner = CourseAccess.IsOwner(course, cmd.RequestedById);
        var isSelf = cmd.TeacherId == cmd.RequestedById;

        if (!isOwner && !isSelf)
            return ApiResponse.Fail("Only the course owner can remove another teacher.");

        if (cmd.TeacherId == course.TeacherId)
            return ApiResponse.Fail("The course owner cannot be removed.");

        var row = await uow.GetRepository<CourseTeacher>()
            .FirstOrDefaultAsync(t => t.CourseId == cmd.CourseId && t.UserId == cmd.TeacherId, ct);

        if (row is null) return ApiResponse.Fail("They are not teaching this course.");

        uow.GetRepository<CourseTeacher>().Delete(row);
        await uow.SaveChangesAsync(ct);

        if (!isSelf)
        {
            await sender.Send(new SendNotificationCommand(
                UserId: cmd.TeacherId,
                Title: $"Removed from {course.Title}",
                Body: $"You are no longer teaching {course.CourseCode}.",
                Type: NotificationType.General,
                RedirectUrl: "/courses"
            ), ct);
        }

        return ApiResponse.Ok(isSelf ? "You have left the course." : "Teacher removed.");
    }
}
