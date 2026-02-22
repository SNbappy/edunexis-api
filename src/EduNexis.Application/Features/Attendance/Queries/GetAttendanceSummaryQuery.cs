using EduNexis.Application.DTOs;

namespace EduNexis.Application.Features.Attendance.Queries;

public record GetAttendanceSummaryQuery(
    Guid CourseId,
    Guid? StudentId = null
) : IQuery<ApiResponse<List<AttendanceSummaryDto>>>;

public sealed class GetAttendanceSummaryQueryHandler(
    IUnitOfWork uow
) : IQueryHandler<GetAttendanceSummaryQuery, ApiResponse<List<AttendanceSummaryDto>>>
{
    public async ValueTask<ApiResponse<List<AttendanceSummaryDto>>> Handle(
        GetAttendanceSummaryQuery query, CancellationToken ct)
    {
        var members = await uow.CourseMembers.GetByCourseAsync(query.CourseId, ct);
        var activeMembers = members.Where(m => m.IsActive).ToList();

        if (query.StudentId.HasValue)
            activeMembers = activeMembers
                .Where(m => m.UserId == query.StudentId.Value).ToList();

        var sessions = await uow.GetRepository<AttendanceSession>()
            .FindAsync(s => s.CourseId == query.CourseId, ct);

        var totalSessions = sessions.Count();
        var dtos = new List<AttendanceSummaryDto>();

        foreach (var member in activeMembers)
        {
            var user = await uow.Users.GetWithProfileAsync(member.UserId, ct);

            var allRecords = await uow.GetRepository<AttendanceRecord>()
                .FindAsync(r => sessions.Select(s => s.Id).Contains(r.SessionId)
                    && r.StudentId == member.UserId, ct);

            var present = allRecords.Count(r => r.Status == AttendanceStatus.Present);
            var absent = allRecords.Count(r => r.Status == AttendanceStatus.Absent);
            var late = allRecords.Count(r => r.Status == AttendanceStatus.Late);
            var percent = totalSessions > 0
                ? Math.Round((decimal)(present + late) / totalSessions * 100, 2)
                : 0;

            dtos.Add(new AttendanceSummaryDto(
                member.UserId,
                user?.Profile?.FullName ?? "Unknown",
                totalSessions, present, absent, late, percent));
        }

        return ApiResponse<List<AttendanceSummaryDto>>.Ok(dtos);
    }
}
