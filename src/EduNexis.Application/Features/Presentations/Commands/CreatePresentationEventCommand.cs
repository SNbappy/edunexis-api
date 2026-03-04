using EduNexis.Application.Features.Presentations.Queries;

namespace EduNexis.Application.Features.Presentations.Commands;

public record CreatePresentationEventCommand(
    Guid CourseId,
    Guid TeacherId,
    string Title,
    string? Description,
    decimal TotalMarks,
    DateTime? ScheduledDate,
    string Format,
    string? Venue,
    bool TopicsAllowed,
    int? DurationPerGroupMinutes
) : ICommand<ApiResponse<PresentationEventDto>>;

public sealed class CreatePresentationEventCommandValidator : AbstractValidator<CreatePresentationEventCommand>
{
    public CreatePresentationEventCommandValidator()
    {
        RuleFor(x => x.Title).NotEmpty().MaximumLength(200);
        RuleFor(x => x.TotalMarks).GreaterThan(0);
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
            throw new UnauthorizedException("Only the teacher can create presentations.");

        var format = Enum.Parse<PresentationFormat>(command.Format);

        var ev = PresentationEvent.Create(
            command.CourseId, command.Title, command.Description,
            command.TotalMarks, command.ScheduledDate, format,
            command.Venue, command.TopicsAllowed, command.DurationPerGroupMinutes,
            command.TeacherId);

        await uow.GetRepository<PresentationEvent>().AddAsync(ev, ct);
        await uow.SaveChangesAsync(ct);

        return ApiResponse<PresentationEventDto>.Ok(PresentationEventDto.From(ev, null, 0));
    }
}

