using EduNexis.Application.DTOs;

namespace EduNexis.Application.Features.Attendance.Queries;

public record GetAttendanceSessionsQuery(Guid CourseId)
    : IQuery<ApiResponse<List<AttendanceSessionDto>>>;

public sealed class GetAttendanceSessionsQueryHandler(
    IUnitOfWork uow
) : IQueryHandler<GetAttendanceSessionsQuery, ApiResponse<List<AttendanceSessionDto>>>
{
    public async ValueTask<ApiResponse<List<AttendanceSessionDto>>> Handle(
        GetAttendanceSessionsQuery query, CancellationToken ct)
    {
        var sessions = await uow.GetRepository<AttendanceSession>()
            .FindAsync(s => s.CourseId == query.CourseId, ct);

        var dtos = new List<AttendanceSessionDto>();
        foreach (var session in sessions.OrderByDescending(s => s.Date))
        {
            var records = await uow.GetRepository<AttendanceRecord>()
                .FindAsync(r => r.SessionId == session.Id, ct);

            var allProfiles = await uow.UserProfiles.GetAllAsync(ct);
            var profileMap = allProfiles.ToDictionary(p => p.UserId, p => p.FullName);

            var recordDtos = records.Select(r => new AttendanceRecordDto(
                r.StudentId,
                profileMap.TryGetValue(r.StudentId, out var name) ? name : "Unknown",
                r.Status.ToString()
            )).ToList();

            dtos.Add(new AttendanceSessionDto(
                session.Id, session.CourseId, session.Date, session.Topic, recordDtos));
        }

        return ApiResponse<List<AttendanceSessionDto>>.Ok(dtos);
    }
}
