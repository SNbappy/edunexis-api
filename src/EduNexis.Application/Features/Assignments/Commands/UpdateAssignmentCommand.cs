using EduNexis.Application.Features.Notifications.Commands;
using EduNexis.Application.Abstractions;
﻿using EduNexis.Application.DTOs;

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
) : ICommand<ApiResponse<AssignmentDto>>, ICourseScopedWrite
{
    public ValueTask<Guid?> ResolveCourseIdAsync(IUnitOfWork uow, CancellationToken ct)
        => ValueTask.FromResult<Guid?>(CourseId);
}

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
    IUnitOfWork uow,
    ISender sender
) : ICommandHandler<UpdateAssignmentCommand, ApiResponse<AssignmentDto>>
{
    public async ValueTask<ApiResponse<AssignmentDto>> Handle(
        UpdateAssignmentCommand cmd, CancellationToken ct)
    {
        var course = await uow.Courses.GetByIdAsync(cmd.CourseId, ct)
            ?? throw new NotFoundException("Course", cmd.CourseId);

        if (!await CourseAccess.IsTeacherAsync(uow, course, cmd.RequestedById, ct))
            throw new UnauthorizedException("Only the teacher can update assignments.");

        var assignment = await uow.GetRepository<Assignment>().GetByIdAsync(cmd.AssignmentId, ct)
            ?? throw new NotFoundException("Assignment", cmd.AssignmentId);

        // Captured before the update so the notification can say what changed.
        var oldDeadline = assignment.Deadline;
        var oldMaxMarks = assignment.MaxMarks;

        assignment.Update(cmd.Title, cmd.Instructions, cmd.Deadline,
            cmd.AllowLateSubmission, cmd.MaxMarks, cmd.RubricNotes);

        uow.GetRepository<Assignment>().Update(assignment);
        await uow.SaveChangesAsync(ct);

        // A moved deadline is the change students most need to hear about, and
        // previously it happened in complete silence.
        var deadlineMoved = oldDeadline != assignment.Deadline;
        var marksChanged  = oldMaxMarks != assignment.MaxMarks;

        if (deadlineMoved || marksChanged)
        {
            var what = deadlineMoved
                ? $"New deadline: {assignment.Deadline:MMM dd, yyyy h:mm tt}."
                : $"Now marked out of {assignment.MaxMarks}.";

            var members = await uow.CourseMembers.GetByCourseAsync(cmd.CourseId, ct);
            foreach (var m in members.Where(x => x.IsActive))
            {
                await sender.Send(new SendNotificationCommand(
                    UserId: m.UserId,
                    Title: $"Assignment updated in {course.Title}",
                    Body: $"\"{assignment.Title}\" changed. {what}",
                    Type: NotificationType.AssignmentUpdated,
                    RedirectUrl: $"/courses/{course.Id}/assignments/{assignment.Id}"
                ), ct);
            }
        }

        var subs = (await uow.GetRepository<AssignmentSubmission>()
            .FindAsync(s => s.AssignmentId == assignment.Id, ct)).ToList();
        var subCount = subs.Count;
        var gradedCount = subs.Count(s => s.IsGraded);

        return ApiResponse<AssignmentDto>.Ok(new AssignmentDto(
            assignment.Id, assignment.CourseId, assignment.Title,
            assignment.Instructions, assignment.Deadline,
            assignment.AllowLateSubmission, assignment.MaxMarks,
            assignment.RubricNotes, assignment.ReferenceFileUrl,
            assignment.IsOpen(), subCount, gradedCount, null, null, null, null, assignment.CreatedAt));
    }
}

