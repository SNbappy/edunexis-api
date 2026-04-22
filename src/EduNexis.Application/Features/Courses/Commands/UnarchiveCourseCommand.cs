using EduNexis.Application.Abstractions;

namespace EduNexis.Application.Features.Courses.Commands;

public record UnarchiveCourseCommand(Guid CourseId) : ICommand<ApiResponse>;

public sealed class UnarchiveCourseCommandValidator : AbstractValidator<UnarchiveCourseCommand>
{
    public UnarchiveCourseCommandValidator()
    {
        RuleFor(x => x.CourseId).NotEmpty();
    }
}

public sealed class UnarchiveCourseCommandHandler(
    IUnitOfWork uow,
    ICurrentUserService currentUser
) : ICommandHandler<UnarchiveCourseCommand, ApiResponse>
{
    public async ValueTask<ApiResponse> Handle(
        UnarchiveCourseCommand cmd, CancellationToken ct)
    {
        var userId  = Guid.Parse(currentUser.UserId);
        var isAdmin = currentUser.Role is "Admin" or "SuperAdmin";

        var course = await uow.Courses.GetByIdAsync(cmd.CourseId, ct);
        if (course is null)
            return ApiResponse.Fail("Course not found.");

        if (course.TeacherId != userId && !isAdmin)
            return ApiResponse.Fail("You don't have permission to unarchive this course.");

        try
        {
            course.Unarchive();
        }
        catch (DomainException ex)
        {
            return ApiResponse.Fail(ex.Message);
        }

        uow.Courses.Update(course);
        await uow.SaveChangesAsync(ct);

        return ApiResponse.Ok("Course unarchived.");
    }
}
