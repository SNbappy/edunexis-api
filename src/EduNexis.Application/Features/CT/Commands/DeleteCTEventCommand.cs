namespace EduNexis.Application.Features.CT.Commands;

public record DeleteCTEventCommand(
    Guid CTEventId,
    Guid TeacherId
) : ICommand<ApiResponse>;

public sealed class DeleteCTEventCommandHandler(
    IUnitOfWork uow
) : ICommandHandler<DeleteCTEventCommand, ApiResponse>
{
    public async ValueTask<ApiResponse> Handle(
        DeleteCTEventCommand command, CancellationToken ct)
    {
        var ctEvent = await uow.GetRepository<CTEvent>().GetByIdAsync(command.CTEventId, ct)
            ?? throw new NotFoundException("CTEvent", command.CTEventId);

        var course = await uow.Courses.GetByIdAsync(ctEvent.CourseId, ct)
            ?? throw new NotFoundException("Course", ctEvent.CourseId);

        if (course.TeacherId != command.TeacherId)
            return ApiResponse.Fail("Only the teacher can delete CT events.");

        if (ctEvent.Status == CTStatus.Published)
            return ApiResponse.Fail("Cannot delete a published CT. Unpublish it first.");

        uow.GetRepository<CTEvent>().Delete(ctEvent);
        await uow.SaveChangesAsync(ct);

        return ApiResponse.Ok("CT event deleted successfully.");
    }
}
