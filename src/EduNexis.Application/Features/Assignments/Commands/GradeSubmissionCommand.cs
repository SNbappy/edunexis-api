using EduNexis.Application.Features.Notifications.Commands;
using EduNexis.Application.Abstractions;
using EduNexis.Application.DTOs;


namespace EduNexis.Application.Features.Assignments.Commands;


public record GradeSubmissionCommand(
    Guid SubmissionId,
    Guid TeacherId,
    decimal Marks,
    string? Feedback
) : ICommand<ApiResponse<SubmissionDto>>, ICourseScopedWrite
{
    public async ValueTask<Guid?> ResolveCourseIdAsync(IUnitOfWork uow, CancellationToken ct)
    {
        var sub = await uow.GetRepository<AssignmentSubmission>().GetByIdAsync(SubmissionId, ct);
        if (sub is null) return null;
        var assignment = await uow.GetRepository<Assignment>().GetByIdAsync(sub.AssignmentId, ct);
        return assignment?.CourseId;
    }
}


public sealed class GradeSubmissionCommandValidator : AbstractValidator<GradeSubmissionCommand>
{
    public GradeSubmissionCommandValidator()
    {
        RuleFor(x => x.Marks).GreaterThanOrEqualTo(0);
    }
}


public sealed class GradeSubmissionCommandHandler(
    IUnitOfWork uow,
    ISender sender
) : ICommandHandler<GradeSubmissionCommand, ApiResponse<SubmissionDto>>
{
    public async ValueTask<ApiResponse<SubmissionDto>> Handle(
        GradeSubmissionCommand command, CancellationToken ct)
    {
        var submission = await uow.GetRepository<AssignmentSubmission>()
            .GetByIdAsync(command.SubmissionId, ct)
            ?? throw new NotFoundException("Submission", command.SubmissionId);


        var assignment = await uow.GetRepository<Assignment>()
            .GetByIdAsync(submission.AssignmentId, ct)
            ?? throw new NotFoundException("Assignment", submission.AssignmentId);


        var course = await uow.Courses.GetByIdAsync(assignment.CourseId, ct)
            ?? throw new NotFoundException("Course", assignment.CourseId);


        if (course.TeacherId != command.TeacherId)
            throw new UnauthorizedException("Only the teacher can grade submissions.");


        if (command.Marks > assignment.MaxMarks)
            return ApiResponse<SubmissionDto>.Fail(
                $"Marks cannot exceed max marks ({assignment.MaxMarks}).");


        submission.Grade(command.Marks, command.Feedback);
        uow.GetRepository<AssignmentSubmission>().Update(submission);
        await uow.SaveChangesAsync(ct);


        // Fetch student full name from UserProfile
        var profile = await uow.UserProfiles
            .FirstOrDefaultAsync(p => p.UserId == submission.StudentId, ct);
        var studentName = profile?.FullName ?? "Unknown";

        // Tell the student their work has been marked. Being graded and not
        // knowing it is the single most-noticed gap: the mark exists, the
        // student has no reason to go looking, and finds out days later.
        await sender.Send(new SendNotificationCommand(
            UserId: submission.StudentId,
            Title: $"Your work was marked in {course.Title}",
            Body: $"\"{assignment.Title}\": {command.Marks} out of {assignment.MaxMarks}.",
            Type: NotificationType.AssignmentGraded,
            RedirectUrl: $"/courses/{course.Id}/assignments/{assignment.Id}"
        ), ct);


        return ApiResponse<SubmissionDto>.Ok(new SubmissionDto(
            submission.Id, submission.AssignmentId, submission.StudentId,
            studentName, submission.SubmissionType.ToString(),
            submission.TextContent, submission.FileUrl, submission.LinkUrl,
            submission.SubmittedAt, submission.IsLate, submission.Marks,
            submission.Feedback, submission.IsGraded));
    }
}
