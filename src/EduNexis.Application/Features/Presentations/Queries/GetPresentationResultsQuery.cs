using EduNexis.Application.Features.Presentations.Queries;

namespace EduNexis.Application.Features.Presentations.Queries;

public record GetPresentationResultsQuery(
    Guid PresentationEventId,
    Guid RequesterId
) : IQuery<ApiResponse<List<PresentationResultDto>>>;

public sealed class GetPresentationResultsQueryHandler(
    IUnitOfWork uow
) : IQueryHandler<GetPresentationResultsQuery, ApiResponse<List<PresentationResultDto>>>
{
    public async ValueTask<ApiResponse<List<PresentationResultDto>>> Handle(
        GetPresentationResultsQuery query, CancellationToken ct)
    {
        var presentation = await uow.GetRepository<PresentationEvent>()
            .GetByIdAsync(query.PresentationEventId, ct)
            ?? throw new NotFoundException("PresentationEvent", query.PresentationEventId);

        var course = await uow.Courses.GetByIdAsync(presentation.CourseId, ct)
            ?? throw new NotFoundException("Course", presentation.CourseId);

        bool isTeacher = course.TeacherId == query.RequesterId;

        var marks = await uow.GetRepository<PresentationMark>()
            .FindAsync(m => m.PresentationEventId == query.PresentationEventId, ct);

        if (!isTeacher)
            marks = marks.Where(m => m.StudentId == query.RequesterId).ToList();

        var result = new List<PresentationResultDto>();

        foreach (var m in marks)
        {
            var profile = await uow.GetRepository<UserProfile>()
                .FirstOrDefaultAsync(p => p.UserId == m.StudentId, ct);
            var user = await uow.GetRepository<User>()
                .GetByIdAsync(m.StudentId, ct);

            result.Add(new PresentationResultDto(
                m.Id, m.StudentId,
                profile?.FullName ?? "Unknown",
                user?.Email ?? string.Empty,
                profile?.ProfilePhotoUrl,
                profile?.StudentId,
                m.Topic,
                m.IsAbsent ? null : m.Marks,
                m.IsAbsent,
                m.Feedback,
                m.MarkedAt));
        }

        return ApiResponse<List<PresentationResultDto>>.Ok(result);
    }
}
