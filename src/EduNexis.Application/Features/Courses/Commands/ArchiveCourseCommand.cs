namespace EduNexis.Application.Features.Courses.Commands;

public record ArchiveCourseCommand(
    Guid CourseId,
    Guid RequesterId
) : ICommand<ApiResponse>;

public sealed class ArchiveCourseCommandHandler(
    IUnitOfWork uow
) : ICommandHandler<ArchiveCourseCommand, ApiResponse>
{
    public async ValueTask<ApiResponse> Handle(
        ArchiveCourseCommand command, CancellationToken ct)
    {
        var course = await uow.Courses.GetByIdAsync(command.CourseId, ct)
            ?? throw new NotFoundException("Course", command.CourseId);

        if (course.TeacherId != command.RequesterId)
            throw new UnauthorizedException("Only the course teacher can archive this course.");

        course.Archive();
        uow.Courses.Update(course);
        await uow.SaveChangesAsync(ct);

        return ApiResponse.Ok("Course archived successfully.");
    }
}
