namespace EduNexis.Application.Features.Marks.Queries;

public record MarkBreakdownDto(
    Guid StudentId,
    string StudentName,
    string BreakdownJson,
    decimal FinalMark,
    bool IsPublished
);

public record GetCourseMarksQuery(
    Guid CourseId,
    Guid RequesterId
) : IQuery<ApiResponse<List<MarkBreakdownDto>>>;

public sealed class GetCourseMarksQueryHandler(
    IUnitOfWork uow
) : IQueryHandler<GetCourseMarksQuery, ApiResponse<List<MarkBreakdownDto>>>
{
    public async ValueTask<ApiResponse<List<MarkBreakdownDto>>> Handle(
        GetCourseMarksQuery query, CancellationToken ct)
    {
        var course = await uow.Courses.GetByIdAsync(query.CourseId, ct)
            ?? throw new NotFoundException("Course", query.CourseId);

        var finalMarks = await uow.GetRepository<FinalMark>()
            .FindAsync(fm => fm.CourseId == query.CourseId, ct);

        var result = new List<MarkBreakdownDto>();

        foreach (var fm in finalMarks)
        {
            var profile = await uow.GetRepository<UserProfile>()
                .FirstOrDefaultAsync(p => p.UserId == fm.StudentId, ct);

            result.Add(new MarkBreakdownDto(
                fm.StudentId,
                profile?.FullName ?? "Unknown",
                fm.BreakdownJson,
                fm.FinalMarkValue,
                fm.IsPublished
            ));
        }

        return ApiResponse<List<MarkBreakdownDto>>.Ok(result);
    }
}
