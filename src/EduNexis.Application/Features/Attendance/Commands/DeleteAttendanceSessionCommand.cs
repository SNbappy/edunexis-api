namespace EduNexis.Application.Features.Attendance.Commands;

public record DeleteAttendanceSessionCommand(Guid CourseId, Guid SessionId)
    : ICommand<ApiResponse>;

public sealed class DeleteAttendanceSessionCommandHandler(
    IUnitOfWork uow,
    ICurrentUserService currentUser
) : ICommandHandler<DeleteAttendanceSessionCommand, ApiResponse>
{
    public async ValueTask<ApiResponse> Handle(
        DeleteAttendanceSessionCommand cmd, CancellationToken ct)
    {
        var requesterId = Guid.Parse(currentUser.UserId);

        var course = await uow.Courses.GetByIdAsync(cmd.CourseId, ct);
        if (course is null)
            return ApiResponse.Fail("Course not found.");

        if (course.TeacherId != requesterId && currentUser.Role != "Admin")
            return ApiResponse.Fail("Only teacher can delete attendance sessions.");

        var session = await uow.GetRepository<AttendanceSession>()
            .GetByIdAsync(cmd.SessionId, ct);

        if (session is null || session.CourseId != cmd.CourseId)
            return ApiResponse.Fail("Session not found.");

        var records = await uow.GetRepository<AttendanceRecord>()
            .FindAsync(r => r.SessionId == cmd.SessionId, ct);

        foreach (var record in records)
            uow.GetRepository<AttendanceRecord>().Delete(record);

        uow.GetRepository<AttendanceSession>().Delete(session);
        await uow.SaveChangesAsync(ct);

        return ApiResponse.Ok("Session deleted.");
    }
}
