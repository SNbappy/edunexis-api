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

        var course = await uow.Courses.GetByIdAsync(cmd.Id, ct);
        if (course is null)
            return ApiResponse.Fail("Course not found.");

        // Only the owning teacher can soft-delete their own course.
        if (course.TeacherId != viewerId)
            return ApiResponse.Fail("You don't have permission to delete this course.");

        // Defense in depth: never trust the client's confirmation UI alone.
        // Re-verify both the course code and the teacher's password server-side.
        if (!string.Equals(cmd.CourseCodeConfirmation.Trim(), course.CourseCode, StringComparison.Ordinal))
            return ApiResponse.Fail("Course code confirmation does not match. Nothing was deleted.");

        var teacher = await uow.Users.GetByIdAsync(viewerId, ct)
            ?? throw new NotFoundException("User", viewerId);

        if (!passwordHasher.Verify(cmd.Password, teacher.PasswordHash))
            return ApiResponse.Fail("Incorrect password. Nothing was deleted.");

        // Soft delete: course moves to the teacher's "Recently deleted" list.
        // Restorable within 30 days via RestoreDeletedCourseCommand; after
        // that it becomes eligible for permanent purge. All attendance,
        // grades, and submissions are preserved throughout.
        course.SoftDeleteByOwner();
        uow.Courses.Update(course);
        await uow.SaveChangesAsync(ct);

        return ApiResponse.Ok("Course moved to Recently Deleted. You can restore it within 30 days.");
    }
}