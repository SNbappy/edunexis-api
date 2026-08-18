using EduNexis.Application.Abstractions;
using EduNexis.Application.Features.Notifications.Commands;

namespace EduNexis.Application.Features.Courses.Commands;

/// <summary>
/// Makes an enrolled student a class representative, or takes it away.
///
/// The CourseMember entity has carried IsCR — and several handlers have honoured
/// it — since the beginning, but nothing could ever set it, so no CR has existed.
/// This is the missing half.
///
/// A CR's only extra power is admitting students to the course. That is
/// deliberately narrow: it is the one piece of course admin that is pure
/// clerical work, happens constantly at the start of a semester, and carries no
/// academic judgement. Marks, attendance and materials stay with the teacher.
///
/// A course may have any number of CRs.
/// </summary>
public record SetClassRepresentativeCommand(
    Guid CourseId,
    Guid StudentId,
    bool IsCR
) : ICommand<ApiResponse>, ICourseScopedWrite
{
    public ValueTask<Guid?> ResolveCourseIdAsync(IUnitOfWork uow, CancellationToken ct)
        => ValueTask.FromResult<Guid?>(CourseId);
}

public sealed class SetClassRepresentativeCommandHandler(
    IUnitOfWork uow,
    ICurrentUserService currentUser,
    ISender sender
) : ICommandHandler<SetClassRepresentativeCommand, ApiResponse>
{
    public async ValueTask<ApiResponse> Handle(
        SetClassRepresentativeCommand cmd, CancellationToken ct)
    {
        var requestedBy = Guid.Parse(currentUser.UserId);

        var course = await uow.Courses.GetByIdAsync(cmd.CourseId, ct);
        if (course is null)
            return ApiResponse.Fail("Course not found.");

        // Appointing a CR is a teacher's call. A CR cannot appoint another CR —
        // otherwise the role could be spread across the class without the
        // teacher ever knowing.
        if (course.TeacherId != requestedBy && currentUser.Role != "SuperAdmin")
            return ApiResponse.Fail("Only the course teacher can appoint a class representative.");

        var member = await uow.CourseMembers.GetMemberAsync(cmd.CourseId, cmd.StudentId, ct);
        if (member is null || !member.IsActive)
            return ApiResponse.Fail("That student is not enrolled in this course.");

        if (member.IsCR == cmd.IsCR)
            return ApiResponse.Ok(cmd.IsCR
                ? "Already a class representative."
                : "Not a class representative.");

        if (cmd.IsCR) member.PromoteToCR();
        else member.DemoteFromCR();

        uow.CourseMembers.Update(member);
        await uow.SaveChangesAsync(ct);

        await sender.Send(new SendNotificationCommand(
            UserId: cmd.StudentId,
            Title: cmd.IsCR
                ? $"You are now a CR for {course.Title}"
                : $"You are no longer a CR for {course.Title}",
            Body: cmd.IsCR
                ? "You can now approve join requests for this course."
                : "Your class representative duties for this course have ended.",
            Type: NotificationType.General,
            RedirectUrl: $"/courses/{course.Id}/members"
        ), ct);

        return ApiResponse.Ok(cmd.IsCR
            ? "Class representative appointed."
            : "Class representative removed.");
    }
}
