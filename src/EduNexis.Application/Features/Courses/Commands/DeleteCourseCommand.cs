using EduNexis.Domain.Common;

namespace EduNexis.Application.Features.Courses.Commands;

public record DeleteCourseCommand(Guid Id) : ICommand<ApiResponse>;

public sealed class DeleteCourseCommandHandler(
    IUnitOfWork uow
) : ICommandHandler<DeleteCourseCommand, ApiResponse>
{
    public async ValueTask<ApiResponse> Handle(
        DeleteCourseCommand cmd, CancellationToken ct)
    {
        var course = await uow.Courses.GetByIdAsync(cmd.Id, ct);
        if (course is null)
            return ApiResponse.Fail("Course not found.");

        uow.Courses.Delete(course);
        await uow.SaveChangesAsync(ct);

        return ApiResponse.Ok("Course deleted.");
    }
}
