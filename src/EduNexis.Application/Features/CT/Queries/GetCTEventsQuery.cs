using EduNexis.Application.Features.CT.Commands;

using EduNexis.Application.Abstractions;

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

        bool isTeacher = await CourseAccess.IsTeacherAsync(uow, course, query.RequestedById, ct);
        var member = await uow.CourseMembers.GetMemberAsync(query.CourseId, query.RequestedById, ct);

        if (!isTeacher && (member is null || !member.IsActive))
            return ApiResponse<List<CTEventDto>>.Fail("You are not a member of this course.");

        var events = await uow.GetRepository<CTEvent>()
            .FindAsync(e => e.CourseId == query.CourseId, ct);

        if (!isTeacher)
            events = events.Where(e => e.Status == Domain.Enums.CTStatus.Published);

        var eventList = events.OrderBy(e => e.CTNumber).ToList();
        var eventIds = eventList.Select(e => e.Id).ToList();

        var mySubmissions = !isTeacher && eventIds.Count > 0
            ? (await uow.GetRepository<CTSubmission>().FindAsync(s => eventIds.Contains(s.CTEventId) && s.StudentId == query.RequestedById, ct))
                .ToDictionary(s => s.CTEventId)
            : new Dictionary<Guid, CTSubmission>();

        var result = eventList
            .Select(e => {
                mySubmissions.TryGetValue(e.Id, out var mySub);
                return new CTEventDto(
                    e.Id, e.CourseId, e.CTNumber, e.Title,
                    e.MaxMarks, e.HeldOn, e.Status.ToString(),
                    e.KhataUploaded, e.CreatedAt,
                    isTeacher ? e.BestScriptUrl : null, isTeacher ? e.BestStudentId : null,
                    isTeacher ? e.WorstScriptUrl : null, isTeacher ? e.WorstStudentId : null,
                    isTeacher ? e.AverageScriptUrl : null, isTeacher ? e.AverageStudentId : null,
                    mySub != null ? (mySub.IsAbsent ? 0 : mySub.ObtainedMarks) : null,
                    mySub?.IsAbsent);
            })
            .ToList();

        return ApiResponse<List<CTEventDto>>.Ok(result);
    }
}

