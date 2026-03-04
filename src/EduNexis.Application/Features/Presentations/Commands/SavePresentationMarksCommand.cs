namespace EduNexis.Application.Features.Presentations.Commands;

public record PresentationMarkEntryRequest(
    Guid StudentId,
    decimal? ObtainedMarks,
    bool IsAbsent,
    string? Topic,
    string? Feedback
);

public record SavePresentationMarksCommand(
    Guid PresentationEventId,
    Guid TeacherId,
    List<PresentationMarkEntryRequest> Entries
) : ICommand<ApiResponse>;

public sealed class SavePresentationMarksCommandHandler(
    IUnitOfWork uow
) : ICommandHandler<SavePresentationMarksCommand, ApiResponse>
{
    public async ValueTask<ApiResponse> Handle(
        SavePresentationMarksCommand command, CancellationToken ct)
    {
        var presentation = await uow.GetRepository<PresentationEvent>()
            .GetByIdAsync(command.PresentationEventId, ct)
            ?? throw new NotFoundException("PresentationEvent", command.PresentationEventId);

        var course = await uow.Courses.GetByIdAsync(presentation.CourseId, ct)
            ?? throw new NotFoundException("Course", presentation.CourseId);

        if (course.TeacherId != command.TeacherId)
            throw new UnauthorizedException("Only the teacher can save marks.");

        foreach (var entry in command.Entries)
        {
            var marks = entry.IsAbsent ? 0 : (entry.ObtainedMarks ?? 0);
            if (marks > presentation.MaxMarks)
                marks = presentation.MaxMarks;

            var existing = await uow.GetRepository<PresentationMark>()
                .FirstOrDefaultAsync(m =>
                    m.PresentationEventId == command.PresentationEventId &&
                    m.StudentId == entry.StudentId, ct);

            if (existing is not null)
            {
                existing.Update(marks, entry.IsAbsent, entry.Topic, entry.Feedback);
                uow.GetRepository<PresentationMark>().Update(existing);
            }
            else
            {
                var mark = PresentationMark.Create(
                    command.PresentationEventId, entry.StudentId,
                    marks, entry.IsAbsent, entry.Topic, entry.Feedback);
                await uow.GetRepository<PresentationMark>().AddAsync(mark, ct);
            }
        }

        await uow.SaveChangesAsync(ct);
        return ApiResponse.Ok($"Marks saved for {command.Entries.Count} student(s).");
    }
}
