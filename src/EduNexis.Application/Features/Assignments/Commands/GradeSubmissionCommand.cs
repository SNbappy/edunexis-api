using EduNexis.Application.DTOs;

namespace EduNexis.Application.Features.Assignments.Commands;

public record GradeSubmissionCommand(
    Guid SubmissionId,
    Guid TeacherId,
    decimal Marks,
    string? Feedback
) : ICommand<ApiResponse<SubmissionDto>>;

public sealed class GradeSubmissionCommandValidator : AbstractValidator<GradeSubmissionCommand>
{
    public GradeSubmissionCommandValidator()
    {
        RuleFor(x => x.Marks).GreaterThanOrEqualTo(0);
    }
}

public sealed class GradeSubmissionCommandHandler(
    IUnitOfWork uow
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

        return ApiResponse<SubmissionDto>.Ok(new SubmissionDto(
            submission.Id, submission.AssignmentId, submission.StudentId,
            string.Empty, submission.SubmissionType.ToString(),
            submission.TextContent, submission.FileUrl, submission.LinkUrl,
            submission.SubmittedAt, submission.IsLate, submission.Marks,
            submission.Feedback, submission.IsGraded));
    }
}
