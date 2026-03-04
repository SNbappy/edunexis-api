using EduNexis.Application.Features.CT.Commands;

namespace EduNexis.Application.Features.CT.Queries;

public record GetCTEventsQuery(
    Guid CourseId,
    Guid RequestedById
) : ICommand<ApiResponse<List<CTEventDto>>>;

public sealed class GetCTEventsQueryHandler(
    IUnitOfWork uow
) : ICommandHandler<GetCTEventsQuery, ApiResponse<List<CTEventDto>>>
{
    public async ValueTask<ApiResponse<List<CTEventDto>>> Handle(
        GetCTEventsQuery query, CancellationToken ct)
    {
        var course = await uow.Courses.GetByIdAsync(query.CourseId, ct);
        if (course is null)
            return ApiResponse<List<CTEventDto>>.Fail("Course not found.");

        bool isTeacher = course.TeacherId == query.RequestedById;
        var member = await uow.CourseMembers.GetMemberAsync(query.CourseId, query.RequestedById, ct);

        if (!isTeacher && (member is null || !member.IsActive))
            return ApiResponse<List<CTEventDto>>.Fail("You are not a member of this course.");

        var events = await uow.GetRepository<CTEvent>()
            .FindAsync(e => e.CourseId == query.CourseId, ct);

        if (!isTeacher)
            events = events.Where(e => e.Status == Domain.Enums.CTStatus.Published);

        var result = events
            .OrderBy(e => e.CTNumber)
            .Select(e => new CTEventDto(
                e.Id, e.CourseId, e.CTNumber, e.Title,
                e.MaxMarks, e.HeldOn, e.Status.ToString(),
                e.KhataUploaded, e.CreatedAt,
                e.BestScriptUrl, e.BestStudentId,
                e.WorstScriptUrl, e.WorstStudentId,
                e.AverageScriptUrl, e.AverageStudentId))
            .ToList();

        return ApiResponse<List<CTEventDto>>.Ok(result);
    }
}

