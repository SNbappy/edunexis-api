namespace EduNexis.Application.Features.Courses.Commands;

public record RestoreDeletedCourseCommand(Guid Id) : ICommand<ApiResponse>;

public sealed class RestoreDeletedCourseCommandHandler(
    IUnitOfWork uow,
    ICurrentUserService currentUser
) : ICommandHandler<RestoreDeletedCourseCommand, ApiResponse>
{
    public async ValueTask<ApiResponse> Handle(
        RestoreDeletedCourseCommand cmd, CancellationToken ct)
    {
        var viewerId = Guid.Parse(currentUser.UserId);

        var course = await uow.Courses.GetByIdAsync(cmd.Id, ct);
        if (course is null)
            return ApiResponse.Fail("Course not found.");

        if (course.TeacherId != viewerId)
            return ApiResponse.Fail("You don't have permission to restore this course.");

        if (!course.IsDeletedByOwner)
            return ApiResponse.Fail("This course is not in Recently Deleted.");

        try
        {
            // RestoreByOwner() throws DomainException if the 30-day window has passed.
            course.RestoreByOwner();
        }
        catch (DomainException ex)
        {
            return ApiResponse.Fail(ex.Message);
        }

        uow.Courses.Update(course);
        await uow.SaveChangesAsync(ct);

        return ApiResponse.Ok("Course restored.");
    }
}