namespace EduNexis.Application.Features.Presentations.Commands;

public record DeletePresentationCommand(
    Guid PresentationEventId,
    Guid TeacherId
) : ICommand<ApiResponse>;

public sealed class DeletePresentationCommandHandler(
    IUnitOfWork uow
) : ICommandHandler<DeletePresentationCommand, ApiResponse>
{
    public async ValueTask<ApiResponse> Handle(
        DeletePresentationCommand command, CancellationToken ct)
    {
        var presentation = await uow.GetRepository<PresentationEvent>()
            .GetByIdAsync(command.PresentationEventId, ct)
            ?? throw new NotFoundException("PresentationEvent", command.PresentationEventId);

        var course = await uow.Courses.GetByIdAsync(presentation.CourseId, ct)
            ?? throw new NotFoundException("Course", presentation.CourseId);

        if (course.TeacherId != command.TeacherId)
            throw new UnauthorizedException("Only the teacher can delete presentations.");

        uow.GetRepository<PresentationEvent>().Delete(presentation);
        await uow.SaveChangesAsync(ct);
        return ApiResponse.Ok("Presentation deleted.");
    }
}
