namespace EduNexis.Application.Features.CT.Commands;

public record GradeCTSubmissionCommand(
    Guid CTEventId,
    Guid StudentId,
    Guid TeacherId,
    decimal Marks
) : ICommand<ApiResponse>;

public sealed class GradeCTSubmissionCommandValidator : AbstractValidator<GradeCTSubmissionCommand>
{
    public GradeCTSubmissionCommandValidator()
    {
        RuleFor(x => x.Marks).GreaterThanOrEqualTo(0);
    }
}

public sealed class GradeCTSubmissionCommandHandler(
    IUnitOfWork uow
) : ICommandHandler<GradeCTSubmissionCommand, ApiResponse>
{
    public async ValueTask<ApiResponse> Handle(
        GradeCTSubmissionCommand command, CancellationToken ct)
    {
        var ctEvent = await uow.GetRepository<CTEvent>().GetByIdAsync(command.CTEventId, ct)
            ?? throw new NotFoundException("CTEvent", command.CTEventId);

        var course = await uow.Courses.GetByIdAsync(ctEvent.CourseId, ct)
            ?? throw new NotFoundException("Course", ctEvent.CourseId);

        if (course.TeacherId != command.TeacherId)
            throw new UnauthorizedException("Only the teacher can grade CT submissions.");

        if (command.Marks > ctEvent.MaxMarks)
            return ApiResponse.Fail($"Marks cannot exceed max marks ({ctEvent.MaxMarks}).");

        var submission = await uow.GetRepository<CTSubmission>()
            .FirstOrDefaultAsync(s =>
                s.CTEventId == command.CTEventId &&
                s.StudentId == command.StudentId, ct)
            ?? CTSubmission.Create(command.CTEventId, command.StudentId);

        submission.AssignMarks(command.Marks);

        if (submission.Id == Guid.Empty)
            await uow.GetRepository<CTSubmission>().AddAsync(submission, ct);
        else
            uow.GetRepository<CTSubmission>().Update(submission);

        await uow.SaveChangesAsync(ct);
        return ApiResponse.Ok("CT marks assigned successfully.");
    }
}
