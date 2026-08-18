using EduNexis.Application.Abstractions;

namespace EduNexis.Application.Features.Assignments.Commands;

public record UnpublishAssignmentCommand(
    Guid CourseId,
    Guid AssignmentId,
    Guid TeacherId
) : ICommand<ApiResponse>, ICourseScopedWrite
{
    public ValueTask<Guid?> ResolveCourseIdAsync(IUnitOfWork uow, CancellationToken ct)
        => ValueTask.FromResult<Guid?>(CourseId);
}

public sealed class UnpublishAssignmentCommandHandler(
    IUnitOfWork uow
) : ICommandHandler<UnpublishAssignmentCommand, ApiResponse>
{
    public async ValueTask<ApiResponse> Handle(
        UnpublishAssignmentCommand command, CancellationToken ct)
    {
        var assignment = await uow.GetRepository<Assignment>()
            .GetByIdAsync(command.AssignmentId, ct)
            ?? throw new NotFoundException("Assignment", command.AssignmentId);

        if (assignment.CourseId != command.CourseId)
            return ApiResponse.Fail("Assignment not found in this course.");

        var course = await uow.Courses.GetByIdAsync(command.CourseId, ct)
            ?? throw new NotFoundException("Course", command.CourseId);

        if (!await CourseAccess.IsTeacherAsync(uow, course, command.TeacherId, ct))
            throw new UnauthorizedException("Only the teacher can unpublish assignment marks.");

        assignment.Unpublish();
        uow.GetRepository<Assignment>().Update(assignment);
        await uow.SaveChangesAsync(ct);

        return ApiResponse.Ok("Assignment marks unpublished. Results are now hidden from students.");
    }
}