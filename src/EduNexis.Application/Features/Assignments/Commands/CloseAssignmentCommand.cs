using EduNexis.Application.Abstractions;
using EduNexis.Application.Features.Notifications.Commands;
using Microsoft.Extensions.Logging;

namespace EduNexis.Application.Features.Assignments.Commands;

/// <summary>
/// Closes an assignment and awards 0 to everyone who turned nothing in.
///
/// Deliberately tied to closing rather than to the deadline passing. An
/// assignment that still accepts late work is not finished with, and zeroing a
/// student the course is explicitly still willing to accept would be wrong.
/// Once it is closed nothing more can arrive, so the absence is final and can
/// be recorded as a real mark that counts in the gradebook.
///
/// Drafts count as nothing turned in — attaching a file and never submitting it
/// is exactly the case this is for.
///
/// Re-running is safe: students who already have a submission are skipped, so a
/// teacher reopening and closing again will not overwrite anybody's work.
/// </summary>
public record CloseAssignmentCommand(
    Guid CourseId,
    Guid AssignmentId,
    Guid RequestedById,
    /// <summary>Set false to close without awarding zeros.</summary>
    bool AwardZeroToNonSubmitters = true
) : ICommand<ApiResponse<CloseAssignmentResult>>, ICourseScopedWrite
{
    public ValueTask<Guid?> ResolveCourseIdAsync(IUnitOfWork uow, CancellationToken ct)
        => ValueTask.FromResult<Guid?>(CourseId);
}

public record CloseAssignmentResult(int ZerosAwarded, int AlreadySubmitted);

public sealed class CloseAssignmentCommandHandler(
    IUnitOfWork uow,
    ISender sender,
    ILogger<CloseAssignmentCommandHandler> logger
) : ICommandHandler<CloseAssignmentCommand, ApiResponse<CloseAssignmentResult>>
{
    public async ValueTask<ApiResponse<CloseAssignmentResult>> Handle(
        CloseAssignmentCommand command, CancellationToken ct)
    {
        var assignment = await uow.GetRepository<Assignment>()
            .GetByIdAsync(command.AssignmentId, ct)
            ?? throw new NotFoundException("Assignment", command.AssignmentId);

        if (assignment.CourseId != command.CourseId)
            return ApiResponse<CloseAssignmentResult>.Fail("Assignment not found.");

        var course = await uow.Courses.GetByIdAsync(command.CourseId, ct);
        if (course is null || course.TeacherId != command.RequestedById)
            return ApiResponse<CloseAssignmentResult>.Fail(
                "Only the course teacher can close an assignment.");

        var submissions = (await uow.GetRepository<AssignmentSubmission>()
                .FindAsync(s => s.AssignmentId == command.AssignmentId, ct))
            .ToList();

        // Anyone with a row at all is left alone, draft or not — creating a
        // second submission for them would duplicate the record.
        var haveSomething = submissions.Select(s => s.StudentId).ToHashSet();

        var members = (await uow.CourseMembers
                .FindAsync(m => m.CourseId == command.CourseId && m.IsActive, ct))
            .ToList();

        var zeros = 0;
        if (command.AwardZeroToNonSubmitters)
        {
            foreach (var m in members.Where(m => !haveSomething.Contains(m.UserId)))
            {
                await uow.GetRepository<AssignmentSubmission>().AddAsync(
                    AssignmentSubmission.CreateAutoZero(command.AssignmentId, m.UserId), ct);
                zeros++;
            }

            // A draft never handed in is a non-submission, and once the window
            // has shut it can never become one. Recorded as a zero too, so the
            // gradebook does not silently ignore the student.
            foreach (var draft in submissions.Where(s => !s.IsTurnedIn && !s.IsGraded))
            {
                draft.Grade(0, "Work was attached but never turned in before the assignment closed.");
                uow.GetRepository<AssignmentSubmission>().Update(draft);
                zeros++;
            }
        }

        assignment.Close();
        uow.GetRepository<Assignment>().Update(assignment);
        await uow.SaveChangesAsync(ct);

        logger.LogInformation(
            "Assignment {AssignmentId} closed by {UserId}. {Zeros} automatic zeros awarded.",
            command.AssignmentId, command.RequestedById, zeros);

        // Tell the students who were zeroed. Finding out at the end of term that
        // a missed assignment scored 0 is the failure this avoids.
        if (zeros > 0)
        {
            var zeroed = members
                .Where(m => !haveSomething.Contains(m.UserId))
                .Select(m => m.UserId)
                .Concat(submissions.Where(s => !s.IsTurnedIn).Select(s => s.StudentId));

            foreach (var userId in zeroed.Distinct())
            {
                await sender.Send(new SendNotificationCommand(
                    UserId: userId,
                    Title: $"Marked 0 in {course.Title}",
                    Body: $"\"{assignment.Title}\" closed with nothing turned in.",
                    Type: NotificationType.AssignmentGraded,
                    RedirectUrl: $"/courses/{course.Id}/assignments/{assignment.Id}"
                ), ct);
            }
        }

        return ApiResponse<CloseAssignmentResult>.Ok(
            new CloseAssignmentResult(zeros, haveSomething.Count),
            zeros > 0
                ? $"Assignment closed. {zeros} student{(zeros == 1 ? "" : "s")} marked 0."
                : "Assignment closed.");
    }
}
