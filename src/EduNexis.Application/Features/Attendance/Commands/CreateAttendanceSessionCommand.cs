using EduNexis.Application.DTOs;

namespace EduNexis.Application.Features.Attendance.Commands;

public record AttendanceEntry(Guid StudentId, AttendanceStatus Status);

public record CreateAttendanceSessionCommand(
    Guid CourseId,
    Guid CreatedById,
    DateOnly Date,
    string? Topic,
    List<AttendanceEntry> Entries
) : ICommand<ApiResponse<AttendanceSessionDto>>;

public sealed class CreateAttendanceSessionCommandValidator
    : AbstractValidator<CreateAttendanceSessionCommand>
{
    public CreateAttendanceSessionCommandValidator()
    {
        RuleFor(x => x.CourseId).NotEmpty();
        RuleFor(x => x.CreatedById).NotEmpty();
        RuleFor(x => x.Entries).NotEmpty().WithMessage("At least one attendance entry is required.");
    }
}

public sealed class CreateAttendanceSessionCommandHandler(
    IUnitOfWork uow
) : ICommandHandler<CreateAttendanceSessionCommand, ApiResponse<AttendanceSessionDto>>
{
    public async ValueTask<ApiResponse<AttendanceSessionDto>> Handle(
        CreateAttendanceSessionCommand command, CancellationToken ct)
    {
        var course = await uow.Courses.GetByIdAsync(command.CourseId, ct)
            ?? throw new NotFoundException("Course", command.CourseId);

        bool isTeacher = course.TeacherId == command.CreatedById;
        var member = await uow.CourseMembers.GetMemberAsync(course.Id, command.CreatedById, ct);
        bool isCR = member?.IsCR ?? false;

        if (!isTeacher && !isCR)
            throw new UnauthorizedException("Only teacher or CR can take attendance.");

        var session = AttendanceSession.Create(
            command.CourseId, command.Date, command.Topic, command.CreatedById);

        await uow.GetRepository<AttendanceSession>().AddAsync(session, ct);
        await uow.SaveChangesAsync(ct);

        var records = new List<AttendanceRecord>();
        foreach (var entry in command.Entries)
        {
            var record = AttendanceRecord.Create(session.Id, entry.StudentId, entry.Status);
            await uow.GetRepository<AttendanceRecord>().AddAsync(record, ct);
            records.Add(record);
        }

        await uow.SaveChangesAsync(ct);

        var recordDtos = records.Select(r => new AttendanceRecordDto(
            r.StudentId, string.Empty, r.Status.ToString())).ToList();

        return ApiResponse<AttendanceSessionDto>.Ok(
            new AttendanceSessionDto(session.Id, session.CourseId,
                session.Date, session.Topic, recordDtos));
    }
}
