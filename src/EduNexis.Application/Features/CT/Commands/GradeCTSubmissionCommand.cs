using EduNexis.Application.Abstractions;
﻿namespace EduNexis.Application.Features.CT.Commands;

public record CTMarkEntry(
    Guid StudentId,
    decimal? ObtainedMarks,
    bool IsAbsent,
    string? Remarks
);

public record BulkGradeCTCommand(
    Guid CTEventId,
    Guid TeacherId,
    List<CTMarkEntry> Marks
) : ICommand<ApiResponse>, ICourseScopedWrite
{
    public async ValueTask<Guid?> ResolveCourseIdAsync(IUnitOfWork uow, CancellationToken ct)
    {
        var e = await uow.GetRepository<CTEvent>().GetByIdAsync(CTEventId, ct);
        return e?.CourseId;
    }
}

public sealed class BulkGradeCTCommandValidator : AbstractValidator<BulkGradeCTCommand>
{
    public BulkGradeCTCommandValidator()
    {
        RuleFor(x => x.Marks).NotEmpty();
        RuleForEach(x => x.Marks).ChildRules(entry =>
        {
            entry.RuleFor(e => e.ObtainedMarks)
                .GreaterThanOrEqualTo(0)
                .When(e => !e.IsAbsent && e.ObtainedMarks.HasValue);
        });
    }
}

public sealed class BulkGradeCTCommandHandler(
    IUnitOfWork uow
) : ICommandHandler<BulkGradeCTCommand, ApiResponse>
{
    public async ValueTask<ApiResponse> Handle(
        BulkGradeCTCommand command, CancellationToken ct)
    {
        var ctEvent = await uow.GetRepository<CTEvent>().GetByIdAsync(command.CTEventId, ct)
            ?? throw new NotFoundException("CTEvent", command.CTEventId);

        var course = await uow.Courses.GetByIdAsync(ctEvent.CourseId, ct)
            ?? throw new NotFoundException("Course", ctEvent.CourseId);

        if (!await CourseAccess.IsTeacherAsync(uow, course, command.TeacherId, ct))
            return ApiResponse.Fail("Only the teacher can enter CT marks.");

        if (!ctEvent.KhataUploaded)
            return ApiResponse.Fail("All 3 khata must be uploaded before entering marks.");

        // Check that all enrolled active students have been provided with marks or marked absent
        var members = await uow.CourseMembers.GetByCourseAsync(ctEvent.CourseId, ct);
        var studentMembers = members.Where(m => m.IsActive).ToList();

        if (studentMembers.Count > 0)
        {
            var commandMarkMap = command.Marks.ToDictionary(m => m.StudentId);
            var unmarkedCount = studentMembers.Count(s =>
                !commandMarkMap.TryGetValue(s.UserId, out var entry) ||
                (!entry.IsAbsent && !entry.ObtainedMarks.HasValue));

            if (unmarkedCount > 0)
                return ApiResponse.Fail($"Cannot save marks: {unmarkedCount} student(s) have not been marked yet. All students must have marks entered or be marked absent.");
        }

        var existingSubmissions = await uow.GetRepository<CTSubmission>()
            .FindAsync(s => s.CTEventId == command.CTEventId, ct);
        var submissionMap = existingSubmissions.ToDictionary(s => s.StudentId);

        var studentIds = command.Marks.Select(m => m.StudentId).Distinct().ToList();
        var profiles = await uow.UserProfiles.FindAsync(p => studentIds.Contains(p.UserId), ct);
        var profileMap = profiles.ToDictionary(p => p.UserId);

        foreach (var entry in command.Marks)
        {
            if (!entry.IsAbsent && entry.ObtainedMarks.HasValue && (entry.ObtainedMarks.Value < 0 || entry.ObtainedMarks > ctEvent.MaxMarks))
            {
                var studentLabel = profileMap.TryGetValue(entry.StudentId, out var prof) && !string.IsNullOrWhiteSpace(prof.StudentId)
                    ? $"{prof.StudentId} ({prof.FullName})"
                    : profileMap.TryGetValue(entry.StudentId, out var prof2) && !string.IsNullOrWhiteSpace(prof2.FullName)
                        ? prof2.FullName
                        : entry.StudentId.ToString();

                return ApiResponse.Fail($"Marks for student {studentLabel} must be between 0 and max marks ({ctEvent.MaxMarks:0.##}).");
            }

            if (!submissionMap.TryGetValue(entry.StudentId, out var submission))
            {
                submission = CTSubmission.Create(command.CTEventId, entry.StudentId);
                await uow.GetRepository<CTSubmission>().AddAsync(submission, ct);
                submissionMap[entry.StudentId] = submission;
            }

            if (entry.IsAbsent)
                submission.MarkAbsent(entry.Remarks);
            else if (entry.ObtainedMarks.HasValue)
                submission.AssignMarks(entry.ObtainedMarks.Value, entry.Remarks);
        }

        await uow.SaveChangesAsync(ct);
        return ApiResponse.Ok("CT marks saved successfully.");
    }
}
