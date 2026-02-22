namespace EduNexis.Application.Features.CT.Commands;

public record CTEventDto(Guid Id, Guid CourseId, string Title, decimal MaxMarks, DateTime CreatedAt);

public record CreateCTEventCommand(
    Guid CourseId,
    Guid TeacherId,
    string Title,
    decimal MaxMarks
) : ICommand<ApiResponse<CTEventDto>>;

public sealed class CreateCTEventCommandValidator : AbstractValidator<CreateCTEventCommand>
{
    public CreateCTEventCommandValidator()
    {
        RuleFor(x => x.Title).NotEmpty().MaximumLength(100);
        RuleFor(x => x.MaxMarks).GreaterThan(0);
    }
}

public sealed class CreateCTEventCommandHandler(
    IUnitOfWork uow
) : ICommandHandler<CreateCTEventCommand, ApiResponse<CTEventDto>>
{
    public async ValueTask<ApiResponse<CTEventDto>> Handle(
        CreateCTEventCommand command, CancellationToken ct)
    {
        var course = await uow.Courses.GetByIdAsync(command.CourseId, ct)
            ?? throw new NotFoundException("Course", command.CourseId);

        if (course.TeacherId != command.TeacherId)
            throw new UnauthorizedException("Only the teacher can create CT events.");

        var ctEvent = CTEvent.Create(
            command.CourseId, command.Title, command.MaxMarks, command.TeacherId);

        await uow.GetRepository<CTEvent>().AddAsync(ctEvent, ct);
        await uow.SaveChangesAsync(ct);

        return ApiResponse<CTEventDto>.Ok(
            new CTEventDto(ctEvent.Id, ctEvent.CourseId,
                ctEvent.Title, ctEvent.MaxMarks, ctEvent.CreatedAt));
    }
}