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
    string? JoiningCode,      // null when viewer is not the course owner
    Guid TeacherId,
    string TeacherName,
    string? TeacherProfilePhotoUrl,
    bool IsArchived,
    int MemberCount,
    DateTime CreatedAt,
    string ViewerRole         // "Owner" | "Member" | "None"
);

public record CourseSummaryDto(
    Guid Id,
    string Title,
    string CourseCode,
    string Department,
    string AcademicSession,
    string Semester,
    string CourseType,
    string CoverImageUrl,
    Guid TeacherId,
    string TeacherName,
    string? TeacherProfilePhotoUrl,
    bool IsArchived,
    int MemberCount,
    DateTime CreatedAt
);

public record PendingCourseDto(
    Guid Id,
    string Title,
    string CourseCode,
    string Department,
    string Semester,
    string CourseType,
    string TeacherName,
    string? TeacherProfilePhotoUrl,
    Guid RequestId,
    DateTime RequestedAt
);

public record RejectedCourseDto(
    Guid Id,
    string Title,
    string CourseCode,
    string Department,
    string Semester,
    string CourseType,
    string TeacherName,
    string? TeacherProfilePhotoUrl,
    Guid RequestId,
    DateTime RequestedAt,
    DateTime? ReviewedAt
);

public record MyCoursesDto(
    List<CourseSummaryDto> Enrolled,
    List<PendingCourseDto> Pending,
    List<RejectedCourseDto> Rejected
);
