namespace EduNexis.Application.Features.Presentations.Commands;

public record UnpublishPresentationCommand(
    Guid PresentationEventId,
    Guid TeacherId
) : ICommand<ApiResponse>;

public sealed class UnpublishPresentationCommandHandler(
    IUnitOfWork uow
) : ICommandHandler<UnpublishPresentationCommand, ApiResponse>
{
    public async ValueTask<ApiResponse> Handle(
        UnpublishPresentationCommand command, CancellationToken ct)
    {
        var presentation = await uow.GetRepository<PresentationEvent>()
            .GetByIdAsync(command.PresentationEventId, ct)
            ?? throw new NotFoundException("PresentationEvent", command.PresentationEventId);

        var course = await uow.Courses.GetByIdAsync(presentation.CourseId, ct)
            ?? throw new NotFoundException("Course", presentation.CourseId);

        if (course.TeacherId != command.TeacherId)
            throw new UnauthorizedException("Only the teacher can unpublish.");

        presentation.Unpublish();
        uow.GetRepository<PresentationEvent>().Update(presentation);
        await uow.SaveChangesAsync(ct);

        return ApiResponse.Ok("Unpublished. Students will no longer see marks.");
    }
}