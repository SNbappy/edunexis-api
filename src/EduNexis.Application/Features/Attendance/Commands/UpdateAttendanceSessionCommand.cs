using EduNexis.Application.DTOs;

namespace EduNexis.Application.Features.Attendance.Commands;

public record UpdateAttendanceEntryCommand(
    Guid CourseId,
    Guid SessionId,
    Guid RequesterId,
    string? Topic,
    List<AttendanceEntry> Entries
) : ICommand<ApiResponse<AttendanceSessionDto>>;

public sealed class UpdateAttendanceEntryCommandHandler(
    IUnitOfWork uow
) : ICommandHandler<UpdateAttendanceEntryCommand, ApiResponse<AttendanceSessionDto>>
{
    public async ValueTask<ApiResponse<AttendanceSessionDto>> Handle(
        UpdateAttendanceEntryCommand cmd, CancellationToken ct)
    {
        var course = await uow.Courses.GetByIdAsync(cmd.CourseId, ct)
            ?? throw new NotFoundException("Course", cmd.CourseId);

        bool isTeacher = course.TeacherId == cmd.RequesterId;
        var member = await uow.CourseMembers.GetMemberAsync(course.Id, cmd.RequesterId, ct);
        bool isCR = member?.IsCR ?? false;

        if (!isTeacher && !isCR)
            throw new UnauthorizedException("Only teacher or CR can edit attendance.");

        var session = await uow.GetRepository<AttendanceSession>()
            .GetByIdAsync(cmd.SessionId, ct);

        if (session is null || session.CourseId != cmd.CourseId)
            return ApiResponse<AttendanceSessionDto>.Fail("Session not found.");

        if (cmd.Topic is not null)
            session.UpdateTopic(cmd.Topic);

        // Delete existing records and recreate
        var existing = await uow.GetRepository<AttendanceRecord>()
            .FindAsync(r => r.SessionId == cmd.SessionId, ct);

        foreach (var r in existing)
            uow.GetRepository<AttendanceRecord>().Delete(r);

        await uow.SaveChangesAsync(ct);

        var records = new List<AttendanceRecord>();
        foreach (var entry in cmd.Entries)
        {
            var record = AttendanceRecord.Create(session.Id, entry.StudentId, entry.Status);
            await uow.GetRepository<AttendanceRecord>().AddAsync(record, ct);
            records.Add(record);
        }

        await uow.SaveChangesAsync(ct);

        var allProfiles = await uow.UserProfiles.GetAllAsync(ct);
        var profileMap = allProfiles.ToDictionary(p => p.UserId, p => p.FullName);

        var recordDtos = records.Select(r => new AttendanceRecordDto(
            r.StudentId,
            profileMap.TryGetValue(r.StudentId, out var name) ? name : "Unknown",
            r.Status.ToString()
        )).ToList();

        return ApiResponse<AttendanceSessionDto>.Ok(
            new AttendanceSessionDto(session.Id, session.CourseId,
                session.Date, session.Topic, recordDtos));
    }
}
