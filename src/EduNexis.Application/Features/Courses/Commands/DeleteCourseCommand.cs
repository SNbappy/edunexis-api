using EduNexis.Domain.Interfaces.Services;

namespace EduNexis.Application.Features.Courses.Commands;

public record DeleteCourseCommand(
    Guid Id,
    string Password,
    string CourseCodeConfirmation
) : ICommand<ApiResponse>;

public sealed class DeleteCourseCommandValidator : AbstractValidator<DeleteCourseCommand>
{
    public DeleteCourseCommandValidator()
    {
        RuleFor(x => x.Password)
            .NotEmpty().WithMessage("Password confirmation is required to delete a course.");

        RuleFor(x => x.CourseCodeConfirmation)
            .NotEmpty().WithMessage("Type the course code to confirm deletion.");
    }
}

public sealed class DeleteCourseCommandHandler(
    IUnitOfWork uow,
    ICurrentUserService currentUser,
    IPasswordHasher passwordHasher
) : ICommandHandler<DeleteCourseCommand, ApiResponse>
{
    public async ValueTask<ApiResponse> Handle(
        DeleteCourseCommand cmd, CancellationToken ct)
    {
        var viewerId = Guid.Parse(currentUser.UserId);
        var isAdmin  = currentUser.Role is "SuperAdmin" or "DepartmentAdmin";

        // Hard delete is intentionally restricted to admins only. Teachers use
        // Archive/Unarchive, which preserves all records (attendance, grades,
        // submissions) and is fully reversible. This endpoint is for admin-driven
        // permanent removal of courses that genuinely should not exist.
        if (!isAdmin)
            return ApiResponse.Fail("Only an administrator can permanently delete a course. Use Archive instead.");

        var course = await uow.Courses.GetByIdAsync(cmd.Id, ct);
        if (course is null)
            return ApiResponse.Fail("Course not found.");

        // Defense in depth: never trust the client's confirmation UI alone.
        // Re-verify both the course code and the admin's password server-side.
        if (!string.Equals(cmd.CourseCodeConfirmation.Trim(), course.CourseCode, StringComparison.Ordinal))
            return ApiResponse.Fail("Course code confirmation does not match. Nothing was deleted.");

        var admin = await uow.Users.GetByIdAsync(viewerId, ct)
            ?? throw new NotFoundException("User", viewerId);

        if (!passwordHasher.Verify(cmd.Password, admin.PasswordHash))
            return ApiResponse.Fail("Incorrect password. Nothing was deleted.");

        // Cascading behavior for related entities is defined in AppDbContext:
        // members, attendance sessions, materials, assignments, etc. will
        // cascade-delete per EF's default behavior for owning relationships.
        uow.Courses.Delete(course);
        await uow.SaveChangesAsync(ct);

        return ApiResponse.Ok("Course permanently deleted.");
    }
}