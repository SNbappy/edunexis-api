namespace EduNexis.Application.Features.Presentations.Commands;

public record UpdatePresentationStatusCommand(
    Guid PresentationEventId,
    Guid TeacherId,
    string Status
) : ICommand<ApiResponse>;

public sealed class UpdatePresentationStatusCommandHandler(
    IUnitOfWork uow
) : ICommandHandler<UpdatePresentationStatusCommand, ApiResponse>
{
    public async ValueTask<ApiResponse> Handle(
        UpdatePresentationStatusCommand command, CancellationToken ct)
    {
        var presentation = await uow.GetRepository<PresentationEvent>()
            .GetByIdAsync(command.PresentationEventId, ct)
            ?? throw new NotFoundException("PresentationEvent", command.PresentationEventId);

        var course = await uow.Courses.GetByIdAsync(presentation.CourseId, ct)
            ?? throw new NotFoundException("Course", presentation.CourseId);

        if (course.TeacherId != command.TeacherId)
            throw new UnauthorizedException("Only the teacher can update presentation status.");

        if (!Enum.TryParse<PresentationStatus>(command.Status, out var status))
            return ApiResponse.Fail($"Invalid status: {command.Status}");

        presentation.UpdateStatus(status);
        uow.GetRepository<PresentationEvent>().Update(presentation);
        await uow.SaveChangesAsync(ct);

        return ApiResponse.Ok("Status updated.");
    }
}
