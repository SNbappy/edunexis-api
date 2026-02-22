namespace EduNexis.Application.DTOs;

public record AttendanceSessionDto(
    Guid Id,
    Guid CourseId,
    DateOnly Date,
    string? Topic,
    List<AttendanceRecordDto> Records
);

public record AttendanceRecordDto(
    Guid StudentId,
    string StudentName,
    string Status
);

public record AttendanceSummaryDto(
    Guid StudentId,
    string StudentName,
    int TotalSessions,
    int PresentCount,
    int AbsentCount,
    int LateCount,
    decimal AttendancePercent
);
