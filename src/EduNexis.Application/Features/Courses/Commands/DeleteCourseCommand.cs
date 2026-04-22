namespace EduNexis.Application.Features.Courses.Commands;

public record DeleteCourseCommand(Guid Id) : ICommand<ApiResponse>;

public sealed class DeleteCourseCommandHandler(
    IUnitOfWork uow,
    ICurrentUserService currentUser
) : ICommandHandler<DeleteCourseCommand, ApiResponse>
{
    public async ValueTask<ApiResponse> Handle(
        DeleteCourseCommand cmd, CancellationToken ct)
    {
        var viewerId = Guid.Parse(currentUser.UserId);
        var isAdmin  = currentUser.Role is "Admin" or "SuperAdmin";

        var course = await uow.Courses.GetByIdAsync(cmd.Id, ct);
        if (course is null)
            return ApiResponse.Fail("Course not found.");

        // Authorization: only the course owner or an admin can delete.
        if (course.TeacherId != viewerId && !isAdmin)
            return ApiResponse.Fail("You don't have permission to delete this course.");

        // Hard delete. This is intentional — the teacher-facing UI exposes only
        // Archive/Unarchive, which preserves all records (attendance, grades,
        // submissions). This endpoint is for admin-driven removal of courses
        // that shouldn't exist (duplicates, test data, etc.).
        //
        // Cascading behavior for related entities is defined in AppDbContext:
        // members, attendance sessions, materials, assignments, etc. will
        // cascade-delete per EF's default behavior for owning relationships.
        uow.Courses.Delete(course);
        await uow.SaveChangesAsync(ct);

        return ApiResponse.Ok("Course deleted.");
    }
}
