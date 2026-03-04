using EduNexis.Application.Features.Presentations.Commands;

namespace EduNexis.Application.Features.Presentations.Queries;

public record PresentationResultDto(
    Guid Id,
    Guid StudentId,
    string StudentName,
    string StudentEmail,
    string? StudentPhoto,
    string? RollNumber,
    string? Topic,
    decimal? ObtainedMarks,
    bool IsAbsent,
    string? Feedback,
    DateTime? GradedAt
);

public record PresentationEventDto(
    Guid Id,
    Guid CourseId,
    string Title,
    string? Description,
    decimal TotalMarks,
    string? ScheduledDate,
    string Status,
    string Format,
    string? Venue,
    bool TopicsAllowed,
    int? DurationPerGroupMinutes,
    string CreatedAt,
    PresentationResultDto? MyResult,
    int SubmittedCount
)
{
    public static PresentationEventDto From(
        PresentationEvent ev,
        PresentationResultDto? myResult,
        int submittedCount) => new(
            ev.Id, ev.CourseId, ev.Title, ev.Description, ev.MaxMarks,
            ev.ScheduledDate?.ToString("O"),
            ev.Status.ToString(), ev.Format.ToString(),
            ev.Venue, ev.TopicsAllowed, ev.DurationPerGroupMinutes,
            ev.CreatedAt.ToString("O"),
            myResult, submittedCount);
}

public record GetPresentationsQuery(
    Guid CourseId,
    Guid RequesterId
) : IQuery<ApiResponse<List<PresentationEventDto>>>;

public sealed class GetPresentationsQueryHandler(
    IUnitOfWork uow
) : IQueryHandler<GetPresentationsQuery, ApiResponse<List<PresentationEventDto>>>
{
    public async ValueTask<ApiResponse<List<PresentationEventDto>>> Handle(
        GetPresentationsQuery query, CancellationToken ct)
    {
        var course = await uow.Courses.GetByIdAsync(query.CourseId, ct)
            ?? throw new NotFoundException("Course", query.CourseId);

        bool isTeacher = course.TeacherId == query.RequesterId;

        var member = await uow.CourseMembers.GetMemberAsync(query.CourseId, query.RequesterId, ct);
        if (!isTeacher && (member is null || !member.IsActive))
            return ApiResponse<List<PresentationEventDto>>.Fail("You are not a member of this course.");

        var events = await uow.GetRepository<PresentationEvent>()
            .FindAsync(e => e.CourseId == query.CourseId, ct);

        var result = new List<PresentationEventDto>();

        foreach (var ev in events.OrderByDescending(e => e.ScheduledDate ?? e.CreatedAt))
        {
            PresentationResultDto? myResult = null;
            int submittedCount = 0;

            if (isTeacher)
            {
                var marks = await uow.GetRepository<PresentationMark>()
                    .FindAsync(m => m.PresentationEventId == ev.Id, ct);
                submittedCount = marks.Count(m => !m.IsAbsent);
            }
            else
            {
                var mark = await uow.GetRepository<PresentationMark>()
                    .FirstOrDefaultAsync(m =>
                        m.PresentationEventId == ev.Id &&
                        m.StudentId == query.RequesterId, ct);

                if (mark is not null)
                {
                    myResult = new PresentationResultDto(
                        mark.Id, mark.StudentId, string.Empty, string.Empty,
                        null, null, mark.Topic,
                        mark.IsAbsent ? null : mark.Marks,
                        mark.IsAbsent, mark.Feedback, mark.MarkedAt);
                }
            }

            result.Add(PresentationEventDto.From(ev, myResult, submittedCount));
        }

        return ApiResponse<List<PresentationEventDto>>.Ok(result);
    }
}
