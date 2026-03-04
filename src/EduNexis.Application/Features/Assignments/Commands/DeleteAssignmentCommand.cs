namespace EduNexis.Application.Features.Assignments.Commands;

public record DeleteAssignmentCommand(
    Guid CourseId,
    Guid AssignmentId,
    Guid RequestedById
) : ICommand<ApiResponse>;

public sealed class DeleteAssignmentCommandHandler(
    IUnitOfWork uow
) : ICommandHandler<DeleteAssignmentCommand, ApiResponse>
{
    public async ValueTask<ApiResponse> Handle(
        DeleteAssignmentCommand cmd, CancellationToken ct)
    {
        var course = await uow.Courses.GetByIdAsync(cmd.CourseId, ct)
            ?? throw new NotFoundException("Course", cmd.CourseId);

        if (course.TeacherId != cmd.RequestedById)
            throw new UnauthorizedException("Only the teacher can delete assignments.");

        var assignment = await uow.GetRepository<Assignment>().GetByIdAsync(cmd.AssignmentId, ct);

        if (assignment is null || assignment.IsDeleted || assignment.CourseId != cmd.CourseId)
            return ApiResponse.Fail("Assignment not found.");

        assignment.Delete();
        uow.GetRepository<Assignment>().Update(assignment);
        await uow.SaveChangesAsync(ct);

        return ApiResponse.Ok("Assignment deleted.");
    }
}
