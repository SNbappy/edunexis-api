namespace EduNexis.Application.Features.Presentations.Commands;

public record GradePresentationCommand(
    Guid PresentationEventId,
    Guid StudentId,
    Guid TeacherId,
    decimal Marks,
    string? Feedback
) : ICommand<ApiResponse>;

public sealed class GradePresentationCommandValidator : AbstractValidator<GradePresentationCommand>
{
    public GradePresentationCommandValidator()
    {
        RuleFor(x => x.Marks).GreaterThanOrEqualTo(0);
    }
}

public sealed class GradePresentationCommandHandler(
    IUnitOfWork uow
) : ICommandHandler<GradePresentationCommand, ApiResponse>
{
    public async ValueTask<ApiResponse> Handle(
        GradePresentationCommand command, CancellationToken ct)
    {
        var presentation = await uow.GetRepository<PresentationEvent>()
            .GetByIdAsync(command.PresentationEventId, ct)
            ?? throw new NotFoundException("PresentationEvent", command.PresentationEventId);

        var course = await uow.Courses.GetByIdAsync(presentation.CourseId, ct)
            ?? throw new NotFoundException("Course", presentation.CourseId);

        if (course.TeacherId != command.TeacherId)
            throw new UnauthorizedException("Only the teacher can grade presentations.");

        if (command.Marks > presentation.MaxMarks)
            return ApiResponse.Fail($"Marks cannot exceed max marks ({presentation.MaxMarks}).");

        var existing = await uow.GetRepository<PresentationMark>()
            .FirstOrDefaultAsync(m =>
                m.PresentationEventId == command.PresentationEventId &&
                m.StudentId == command.StudentId, ct);

        if (existing is not null)
        {
            existing.Update(command.Marks, command.Feedback);
            uow.GetRepository<PresentationMark>().Update(existing);
        }
        else
        {
            var mark = PresentationMark.Create(
                command.PresentationEventId, command.StudentId,
                command.Marks, command.Feedback);
            await uow.GetRepository<PresentationMark>().AddAsync(mark, ct);
        }

        await uow.SaveChangesAsync(ct);
        return ApiResponse.Ok("Presentation marks assigned successfully.");
    }
}