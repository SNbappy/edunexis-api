using EduNexis.Application.DTOs;

namespace EduNexis.Application.Features.Assignments.Commands;

public record UpdateAssignmentCommand(
    Guid AssignmentId,
    Guid CourseId,
    Guid RequestedById,
    string Title,
    string? Instructions,
    DateTime Deadline,
    bool AllowLateSubmission,
    decimal MaxMarks,
    string? RubricNotes
) : ICommand<ApiResponse<AssignmentDto>>;

public sealed class UpdateAssignmentCommandValidator : AbstractValidator<UpdateAssignmentCommand>
{
    public UpdateAssignmentCommandValidator()
    {
        RuleFor(x => x.Title).NotEmpty().MaximumLength(200);
        RuleFor(x => x.Deadline).GreaterThan(DateTime.UtcNow)
            .WithMessage("Deadline must be in the future.");
        RuleFor(x => x.MaxMarks).GreaterThan(0);
    }
}

public sealed class UpdateAssignmentCommandHandler(
    IUnitOfWork uow
) : ICommandHandler<UpdateAssignmentCommand, ApiResponse<AssignmentDto>>
{
    public async ValueTask<ApiResponse<AssignmentDto>> Handle(
        UpdateAssignmentCommand cmd, CancellationToken ct)
    {
        var course = await uow.Courses.GetByIdAsync(cmd.CourseId, ct)
            ?? throw new NotFoundException("Course", cmd.CourseId);

        if (course.TeacherId != cmd.RequestedById)
            throw new UnauthorizedException("Only the teacher can update assignments.");

        var assignment = await uow.GetRepository<Assignment>().GetByIdAsync(cmd.AssignmentId, ct)
            ?? throw new NotFoundException("Assignment", cmd.AssignmentId);

        assignment.Update(cmd.Title, cmd.Instructions, cmd.Deadline,
            cmd.AllowLateSubmission, cmd.MaxMarks, cmd.RubricNotes);

        uow.GetRepository<Assignment>().Update(assignment);
        await uow.SaveChangesAsync(ct);

        var subCount = (await uow.GetRepository<AssignmentSubmission>()
            .FindAsync(s => s.AssignmentId == assignment.Id, ct)).Count();

        return ApiResponse<AssignmentDto>.Ok(new AssignmentDto(
            assignment.Id, assignment.CourseId, assignment.Title,
            assignment.Instructions, assignment.Deadline,
            assignment.AllowLateSubmission, assignment.MaxMarks,
            assignment.RubricNotes, assignment.ReferenceFileUrl,
            assignment.IsOpen(), subCount, assignment.CreatedAt));
    }
}
