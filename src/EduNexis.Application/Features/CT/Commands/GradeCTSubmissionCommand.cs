namespace EduNexis.Application.Features.CT.Commands;

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
) : ICommand<ApiResponse>;

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

        if (course.TeacherId != command.TeacherId)
            return ApiResponse.Fail("Only the teacher can enter CT marks.");

        if (!ctEvent.KhataUploaded)
            return ApiResponse.Fail("All 3 khata must be uploaded before entering marks.");

        var existingSubmissions = await uow.GetRepository<CTSubmission>()
            .FindAsync(s => s.CTEventId == command.CTEventId, ct);
        var submissionMap = existingSubmissions.ToDictionary(s => s.StudentId);

        foreach (var entry in command.Marks)
        {
            if (!entry.IsAbsent && entry.ObtainedMarks.HasValue && entry.ObtainedMarks > ctEvent.MaxMarks)
                return ApiResponse.Fail($"Marks for student {entry.StudentId} exceed max marks ({ctEvent.MaxMarks}).");

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
