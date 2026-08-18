using EduNexis.Application.Abstractions;
using EduNexis.Application.DTOs;
using EduNexis.Application.Features.Notifications.Commands;

namespace EduNexis.Application.Features.Assignments.Commands;

/*
 * Handing work in, and taking it back.
 *
 * Attaching files and submitting are separate acts, the way they are in Google
 * Classroom. A draft belongs to the student alone; only turning it in makes it
 * visible to the teacher. While the assignment is still open the student may
 * take it back, edit and turn it in again.
 */

// ── Turn in ──────────────────────────────────────────────────────────

public record TurnInAssignmentCommand(
    Guid AssignmentId,
    Guid StudentId
) : ICommand<ApiResponse<SubmissionDto>>, ICourseScopedWrite
{
    public async ValueTask<Guid?> ResolveCourseIdAsync(IUnitOfWork uow, CancellationToken ct)
        => (await uow.GetRepository<Assignment>().GetByIdAsync(AssignmentId, ct))?.CourseId;
}

public sealed class TurnInAssignmentCommandHandler(
    IUnitOfWork uow,
    ISender sender
) : ICommandHandler<TurnInAssignmentCommand, ApiResponse<SubmissionDto>>
{
    public async ValueTask<ApiResponse<SubmissionDto>> Handle(
        TurnInAssignmentCommand command, CancellationToken ct)
    {
        var assignment = await uow.GetRepository<Assignment>()
            .GetByIdAsync(command.AssignmentId, ct)
            ?? throw new NotFoundException("Assignment", command.AssignmentId);

        var submission = await uow.GetRepository<AssignmentSubmission>()
            .FirstOrDefaultAsync(s =>
                s.AssignmentId == command.AssignmentId &&
                s.StudentId == command.StudentId, ct);

        if (submission is null)
            return ApiResponse<SubmissionDto>.Fail("Attach your work before turning it in.");

        if (submission.IsTurnedIn)
            return ApiResponse<SubmissionDto>.Fail("This is already turned in.");

        // Lateness is judged when it is handed in, not when the first draft was
        // saved — a draft started early but submitted late is late.
        var isLate = DateTime.UtcNow > assignment.Deadline;
        if (isLate && !assignment.AllowLateSubmission)
            return ApiResponse<SubmissionDto>.Fail(
                "The deadline has passed and late submissions are not allowed.");

        submission.TurnIn(isLate);
        uow.GetRepository<AssignmentSubmission>().Update(submission);
        await uow.SaveChangesAsync(ct);

        var profile = await uow.UserProfiles
            .FirstOrDefaultAsync(p => p.UserId == command.StudentId, ct);
        var studentName = profile?.FullName ?? "Unknown";

        var course = await uow.Courses.GetByIdAsync(assignment.CourseId, ct);
        if (course is not null)
        {
            await sender.Send(new SendNotificationCommand(
                UserId: course.TeacherId,
                Title: $"New submission in {course.Title}",
                Body: isLate
                    ? $"{studentName} turned in \"{assignment.Title}\" (late)."
                    : $"{studentName} turned in \"{assignment.Title}\".",
                Type: NotificationType.SubmissionReceived,
                RedirectUrl: $"/courses/{course.Id}/assignments/{assignment.Id}"
            ), ct);
        }

        return ApiResponse<SubmissionDto>.Ok(
            await SubmissionMapper.ToDtoAsync(uow, submission, studentName, ct),
            "Turned in.");
    }
}

// ── Unsubmit ─────────────────────────────────────────────────────────

public record UnsubmitAssignmentCommand(
    Guid AssignmentId,
    Guid StudentId
) : ICommand<ApiResponse<SubmissionDto>>, ICourseScopedWrite
{
    public async ValueTask<Guid?> ResolveCourseIdAsync(IUnitOfWork uow, CancellationToken ct)
        => (await uow.GetRepository<Assignment>().GetByIdAsync(AssignmentId, ct))?.CourseId;
}

public sealed class UnsubmitAssignmentCommandHandler(
    IUnitOfWork uow
) : ICommandHandler<UnsubmitAssignmentCommand, ApiResponse<SubmissionDto>>
{
    public async ValueTask<ApiResponse<SubmissionDto>> Handle(
        UnsubmitAssignmentCommand command, CancellationToken ct)
    {
        var assignment = await uow.GetRepository<Assignment>()
            .GetByIdAsync(command.AssignmentId, ct)
            ?? throw new NotFoundException("Assignment", command.AssignmentId);

        var submission = await uow.GetRepository<AssignmentSubmission>()
            .FirstOrDefaultAsync(s =>
                s.AssignmentId == command.AssignmentId &&
                s.StudentId == command.StudentId, ct);

        if (submission is null || !submission.IsTurnedIn)
            return ApiResponse<SubmissionDto>.Fail("There is nothing turned in to take back.");

        // Once marked, it is the teacher's record. Letting a student unsubmit
        // graded work would silently erase a mark that has already been given
        // and, if results are published, already seen.
        if (submission.IsGraded)
            return ApiResponse<SubmissionDto>.Fail(
                "This has already been marked, so it can no longer be taken back.");

        // Past the point where it could be turned in again, unsubmitting would
        // only strand the student with nothing submitted and no way to fix it.
        var isPastDue = DateTime.UtcNow > assignment.Deadline;
        if (isPastDue && !assignment.AllowLateSubmission)
            return ApiResponse<SubmissionDto>.Fail(
                "The deadline has passed, so this can no longer be taken back.");

        submission.Unsubmit();
        uow.GetRepository<AssignmentSubmission>().Update(submission);
        await uow.SaveChangesAsync(ct);

        var profile = await uow.UserProfiles
            .FirstOrDefaultAsync(p => p.UserId == command.StudentId, ct);

        return ApiResponse<SubmissionDto>.Ok(
            await SubmissionMapper.ToDtoAsync(uow, submission, profile?.FullName ?? "Unknown", ct),
            "Taken back. Turn it in again when you are ready.");
    }
}

// ── Shared mapping ───────────────────────────────────────────────────

internal static class SubmissionMapper
{
    public static async Task<SubmissionDto> ToDtoAsync(
        IUnitOfWork uow, AssignmentSubmission s, string studentName, CancellationToken ct)
    {
        var attachments = (await uow.GetRepository<SubmissionAttachment>()
                .FindAsync(a => a.SubmissionId == s.Id, ct))
            .OrderBy(a => a.SortOrder)
            .Select(a => new SubmissionAttachmentDto(
                a.Id, a.Kind.ToString(), a.Url, a.FileName, a.FileSizeBytes))
            .ToList();

        return new SubmissionDto(
            s.Id, s.AssignmentId, s.StudentId, studentName,
            s.SubmissionType.ToString(), s.TextContent, s.FileUrl, s.LinkUrl,
            s.SubmittedAt, s.IsLate, s.Marks, s.Feedback, s.IsGraded,
            attachments, s.IsTurnedIn, s.TurnedInAt, s.IsAutoZero);
    }
}
