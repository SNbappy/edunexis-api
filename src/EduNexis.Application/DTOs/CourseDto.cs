namespace EduNexis.Application.DTOs;

public record CourseDto(
    Guid Id,
    string Title,
    string CourseCode,
    decimal CreditHours,
    string Department,
    string AcademicSession,
    string Semester,
    string? Section,
    string CourseType,
    string? Description,
    string CoverImageUrl,
    string JoiningCode,
    Guid TeacherId,
    string TeacherName,
    bool IsArchived,
    int MemberCount,
    DateTime CreatedAt
);

public record CourseSummaryDto(
    Guid Id,
    string Title,
    string CourseCode,
    string Department,
    string AcademicSession,
    string CoverImageUrl,
    string TeacherName,
    bool IsArchived
);
