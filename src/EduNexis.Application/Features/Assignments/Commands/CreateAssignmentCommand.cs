using EduNexis.Application.DTOs;
using EduNexis.Application.Features.Notifications.Commands;

namespace EduNexis.Application.Features.Assignments.Commands;

public record CreateAssignmentCommand(
    Guid CourseId,
    Guid CreatedById,
    string Title,
    string? Instructions,
    DateTime Deadline,
    bool AllowLateSubmission,
    decimal MaxMarks,
    string? RubricNotes,
    Stream? ReferenceFileStream,
    string? ReferenceFileName
) : ICommand<ApiResponse<AssignmentDto>>;

public sealed class CreateAssignmentCommandValidator : AbstractValidator<CreateAssignmentCommand>
{
    public CreateAssignmentCommandValidator()
    {
        RuleFor(x => x.Title).NotEmpty().MaximumLength(200);
        RuleFor(x => x.Deadline).GreaterThan(DateTime.UtcNow)
            .WithMessage("Deadline must be in the future.");
        RuleFor(x => x.MaxMarks).GreaterThan(0);
    }
}

public sealed class CreateAssignmentCommandHandler(
    IUnitOfWork uow,
    IFileStorageService storage,
    ISender sender
) : ICommandHandler<CreateAssignmentCommand, ApiResponse<AssignmentDto>>
{
    public async ValueTask<ApiResponse<AssignmentDto>> Handle(
        CreateAssignmentCommand command, CancellationToken ct)
    {
        var course = await uow.Courses.GetByIdAsync(command.CourseId, ct)
            ?? throw new NotFoundException("Course", command.CourseId);

        if (course.TeacherId != command.CreatedById)
            throw new UnauthorizedException("Only the teacher can create assignments.");

        string? refFileUrl = null;
        if (command.ReferenceFileStream is not null && command.ReferenceFileName is not null)
        {
            refFileUrl = await storage.UploadAsync(
                command.ReferenceFileStream, command.ReferenceFileName,
                $"assignments/{command.CourseId}", ct);
        }

        var assignment = Assignment.Create(
            command.CourseId, command.Title, command.Instructions,
            command.Deadline, command.AllowLateSubmission,
            command.MaxMarks, command.RubricNotes, refFileUrl, command.CreatedById);

        await uow.GetRepository<Assignment>().AddAsync(assignment, ct);
        await uow.SaveChangesAsync(ct);

        var members = await uow.CourseMembers.GetByCourseAsync(command.CourseId, ct);
        var tasks = members
            .Where(m => m.IsActive)
            .Select(m => sender.Send(new SendNotificationCommand(
                UserId: m.UserId,
                Title: $"New Assignment in {course.Title}",
                Body: $"\"{command.Title}\" is due by {command.Deadline:MMM dd, yyyy}.",
                Type: NotificationType.NewAssignment,
                RedirectUrl: $"/courses/{course.Id}/assignments"
            ), ct).AsTask());

        await Task.WhenAll(tasks);

        return ApiResponse<AssignmentDto>.Ok(new AssignmentDto(
            assignment.Id, assignment.CourseId, assignment.Title,
            assignment.Instructions, assignment.Deadline,
            assignment.AllowLateSubmission, assignment.MaxMarks,
            assignment.RubricNotes, assignment.ReferenceFileUrl,
            assignment.IsOpen(), 0, assignment.CreatedAt));
    }
}
