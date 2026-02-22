namespace EduNexis.Application.Features.Presentations.Commands;

public record PresentationEventDto(Guid Id, Guid CourseId, string Title, decimal MaxMarks, DateTime CreatedAt);

public record CreatePresentationEventCommand(
    Guid CourseId,
    Guid TeacherId,
    string Title,
    decimal MaxMarks
) : ICommand<ApiResponse<PresentationEventDto>>;

public sealed class CreatePresentationEventCommandValidator
    : AbstractValidator<CreatePresentationEventCommand>
{
    public CreatePresentationEventCommandValidator()
    {
        RuleFor(x => x.Title).NotEmpty().MaximumLength(100);
        RuleFor(x => x.MaxMarks).GreaterThan(0);
    }
}

public sealed class CreatePresentationEventCommandHandler(
    IUnitOfWork uow
) : ICommandHandler<CreatePresentationEventCommand, ApiResponse<PresentationEventDto>>
{
    public async ValueTask<ApiResponse<PresentationEventDto>> Handle(
        CreatePresentationEventCommand command, CancellationToken ct)
    {
        var course = await uow.Courses.GetByIdAsync(command.CourseId, ct)
            ?? throw new NotFoundException("Course", command.CourseId);

        if (course.TeacherId != command.TeacherId)
            throw new UnauthorizedException("Only the teacher can create presentation events.");

        var presentation = PresentationEvent.Create(
            command.CourseId, command.Title, command.MaxMarks, command.TeacherId);

        await uow.GetRepository<PresentationEvent>().AddAsync(presentation, ct);
        await uow.SaveChangesAsync(ct);

        return ApiResponse<PresentationEventDto>.Ok(
            new PresentationEventDto(presentation.Id, presentation.CourseId,
                presentation.Title, presentation.MaxMarks, presentation.CreatedAt));
    }
}
