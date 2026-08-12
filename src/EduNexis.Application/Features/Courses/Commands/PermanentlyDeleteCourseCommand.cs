namespace EduNexis.Application.Features.Courses.Commands;

/// <summary>
/// Hard-deletes a course that is already in the teacher's Recently Deleted
/// list. Skips password/code re-confirmation since the course already went
/// through that gate on soft-delete; the UI should still confirm intent.
/// </summary>
public record PermanentlyDeleteCourseCommand(Guid Id) : ICommand<ApiResponse>;

public sealed class PermanentlyDeleteCourseCommandHandler(
    IUnitOfWork uow,
    ICurrentUserService currentUser
) : ICommandHandler<PermanentlyDeleteCourseCommand, ApiResponse>
{
    public async ValueTask<ApiResponse> Handle(
        PermanentlyDeleteCourseCommand cmd, CancellationToken ct)
    {
        var viewerId = Guid.Parse(currentUser.UserId);

        var course = await uow.Courses.GetByIdAsync(cmd.Id, ct);
        if (course is null)
            return ApiResponse.Fail("Course not found.");

        if (course.TeacherId != viewerId)
            return ApiResponse.Fail("You don't have permission to delete this course.");

        if (!course.IsDeletedByOwner)
            return ApiResponse.Fail("Only courses in Recently Deleted can be permanently removed.");

        // Cascading behavior for related entities is defined in AppDbContext:
        // members, attendance sessions, materials, assignments, etc. will
        // cascade-delete per EF's default behavior for owning relationships.
        uow.Courses.Delete(course);
        await uow.SaveChangesAsync(ct);

        return ApiResponse.Ok("Course permanently deleted.");
    }
}