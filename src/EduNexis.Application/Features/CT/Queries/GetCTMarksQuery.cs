using EduNexis.Application.Features.CT.Commands;

namespace EduNexis.Application.Features.CT.Queries;

public record CTMarkDto(
    Guid StudentId,
    string StudentEmail,
    decimal? ObtainedMarks,
    bool IsAbsent,
    string? Remarks,
    DateTime? MarkedAt
);

public record CTMarksResultDto(
    Guid CTEventId,
    int CTNumber,
    string Title,
    decimal MaxMarks,
    string Status,
    string? BestScriptUrl,
    string? WorstScriptUrl,
    string? AverageScriptUrl,
    List<CTMarkDto> Marks,
    decimal? ClassAverage,
    decimal? Highest,
    decimal? Lowest
);

public record GetCTMarksQuery(
    Guid CTEventId,
    Guid RequestedById
) : ICommand<ApiResponse<CTMarksResultDto>>;

public sealed class GetCTMarksQueryHandler(
    IUnitOfWork uow
) : ICommandHandler<GetCTMarksQuery, ApiResponse<CTMarksResultDto>>
{
    public async ValueTask<ApiResponse<CTMarksResultDto>> Handle(
        GetCTMarksQuery query, CancellationToken ct)
    {
        var ctEvent = await uow.GetRepository<CTEvent>().GetByIdAsync(query.CTEventId, ct);
        if (ctEvent is null)
            return ApiResponse<CTMarksResultDto>.Fail("CT not found.");

        var course = await uow.Courses.GetByIdAsync(ctEvent.CourseId, ct);
        if (course is null)
            return ApiResponse<CTMarksResultDto>.Fail("Course not found.");

        bool isTeacher = course.TeacherId == query.RequestedById;
        var member = await uow.CourseMembers.GetMemberAsync(ctEvent.CourseId, query.RequestedById, ct);
        bool isCR = member?.IsCR ?? false;

        if (!isTeacher && (member is null || !member.IsActive))
            return ApiResponse<CTMarksResultDto>.Fail("Access denied.");

        if (!isTeacher && !isCR && ctEvent.Status != Domain.Enums.CTStatus.Published)
            return ApiResponse<CTMarksResultDto>.Fail("CT results are not published yet.");

        var submissions = await uow.GetRepository<CTSubmission>()
            .FindAsync(s => s.CTEventId == query.CTEventId, ct);

        if (!isTeacher && !isCR)
            submissions = submissions.Where(s => s.StudentId == query.RequestedById);

        var markDtos = submissions.Select(s => new CTMarkDto(
            s.StudentId,
            s.Student?.Email ?? s.StudentId.ToString(),
            s.ObtainedMarks,
            s.IsAbsent,
            s.Remarks,
            s.MarkedAt
        )).ToList();

        var gradedMarks = markDtos
            .Where(m => !m.IsAbsent && m.ObtainedMarks.HasValue)
            .Select(m => m.ObtainedMarks!.Value)
            .ToList();

        decimal? avg     = gradedMarks.Count > 0 ? Math.Round(gradedMarks.Average(), 2) : null;
        decimal? highest = gradedMarks.Count > 0 ? gradedMarks.Max() : null;
        decimal? lowest  = gradedMarks.Count > 0 ? gradedMarks.Min() : null;

        return ApiResponse<CTMarksResultDto>.Ok(new CTMarksResultDto(
            ctEvent.Id, ctEvent.CTNumber, ctEvent.Title, ctEvent.MaxMarks,
            ctEvent.Status.ToString(),
            ctEvent.BestScriptUrl, ctEvent.WorstScriptUrl, ctEvent.AverageScriptUrl,
            markDtos, avg, highest, lowest));
    }
}
