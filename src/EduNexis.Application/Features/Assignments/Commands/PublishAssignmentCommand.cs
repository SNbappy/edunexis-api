using EduNexis.Application.Abstractions;
using EduNexis.Application.Features.Notifications.Commands;

namespace EduNexis.Application.Features.Assignments.Commands;

public record PublishAssignmentCommand(
    Guid CourseId,
    Guid AssignmentId,
    Guid TeacherId
) : ICommand<ApiResponse>, ICourseScopedWrite
{
    public ValueTask<Guid?> ResolveCourseIdAsync(IUnitOfWork uow, CancellationToken ct)
        => ValueTask.FromResult<Guid?>(CourseId);
}

public sealed class PublishAssignmentCommandHandler(
    IUnitOfWork uow,
    ISender sender
) : ICommandHandler<PublishAssignmentCommand, ApiResponse>
{
    public async ValueTask<ApiResponse> Handle(
        PublishAssignmentCommand command, CancellationToken ct)
    {
        var assignment = await uow.GetRepository<Assignment>()
            .GetByIdAsync(command.AssignmentId, ct)
            ?? throw new NotFoundException("Assignment", command.AssignmentId);

        if (assignment.CourseId != command.CourseId)
            return ApiResponse.Fail("Assignment not found in this course.");

        var course = await uow.Courses.GetByIdAsync(command.CourseId, ct)
            ?? throw new NotFoundException("Course", command.CourseId);

        if (!await CourseAccess.IsTeacherAsync(uow, course, command.TeacherId, ct))
            throw new UnauthorizedException("Only the teacher can publish assignment marks.");

        if (DateTime.UtcNow < assignment.Deadline)
            return ApiResponse.Fail("Assignment marks cannot be published before the deadline has passed.");

        var teacherIds = await CourseAccess.TeacherIdsAsync(uow, course, ct);
        var members = await uow.CourseMembers
            .FindAsync(m => m.CourseId == command.CourseId && m.IsActive, ct);
        var students = members.Where(m => !teacherIds.Contains(m.UserId)).ToList();

        if (students.Count == 0)
            return ApiResponse.Fail("No active students enrolled in this course.");

        var submissions = (await uow.GetRepository<AssignmentSubmission>()
            .FindAsync(s => s.AssignmentId == command.AssignmentId, ct)).ToList();
        var submissionMap = submissions.ToDictionary(s => s.StudentId);

        // Auto-zero non-submitters and drafts never turned in
        foreach (var student in students)
        {
            if (!submissionMap.TryGetValue(student.UserId, out var sub))
            {
                sub = AssignmentSubmission.CreateAutoZero(command.AssignmentId, student.UserId);
                await uow.GetRepository<AssignmentSubmission>().AddAsync(sub, ct);
                submissionMap[student.UserId] = sub;
            }
            else if (!sub.IsTurnedIn && !sub.IsGraded)
            {
                sub.Grade(0, "Work was attached but never turned in before the deadline.");
                uow.GetRepository<AssignmentSubmission>().Update(sub);
            }
        }

        // Verify all students are graded
        var ungradedStudents = students
            .Where(s => !submissionMap.TryGetValue(s.UserId, out var sub) || !sub.IsGraded)
            .ToList();

        if (ungradedStudents.Count > 0)
        {
            return ApiResponse.Fail($"Cannot publish: {ungradedStudents.Count} student(s) have not been graded yet. All students must have a grade to publish.");
        }

        assignment.Publish();
        uow.GetRepository<Assignment>().Update(assignment);
        await uow.SaveChangesAsync(ct);

        foreach (var student in students)
        {
            submissionMap.TryGetValue(student.UserId, out var sub);
            var marksText = sub?.Marks.HasValue == true ? $"{sub.Marks.Value:0.##} / {assignment.MaxMarks:0.##}" : "Available";
            await sender.Send(new SendNotificationCommand(
                UserId: student.UserId,
                Title: $"Assignment Marks Published — {course.Title}",
                Body: $"Your mark for \"{assignment.Title}\" is {marksText}.",
                Type: NotificationType.AssignmentGraded,
                RedirectUrl: $"/courses/{course.Id}/assignments/{assignment.Id}"
            ), ct);
        }

        return ApiResponse.Ok("Assignment marks published successfully. Students can now view their results.");
    }
}