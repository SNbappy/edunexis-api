namespace EduNexis.Application.DTOs;

public record UserDto(
    Guid Id,
    string Email,
    string Role,
    bool IsProfileComplete,
    UserProfileDto? Profile
);

public record UserProfileDto(
    Guid Id,
    string FullName,
    string? Department,
    string? Designation,
    string? StudentId,
    string? Bio,
    string? ProfilePhotoUrl,
    string? CoverPhotoUrl,
    string? PhoneNumber,
    string? LinkedInUrl,
    string? FacebookUrl,
    string? TwitterUrl,
    string? GitHubUrl,
    string? WebsiteUrl,
    int ProfileCompletionPercent
);

public record UserEducationDto(
    Guid Id,
    string Institution,
    string Degree,
    string FieldOfStudy,
    int StartYear,
    int? EndYear,
    string? Description
);

public record PublicCourseDto(
    Guid Id,
    string Title,
    string CourseCode,
    string Department,
    string Semester,
    string CourseType
);

/// <summary>
/// "Self"       → profile owner viewing their own profile
/// "CourseMate" → viewer and profile owner share an active course enrollment
/// "Stranger"   → any other authenticated viewer
/// </summary>
public record PublicProfileDto(
    Guid UserId,
    string FullName,
    string? Department,
    string? Designation,
    string? StudentId,        // null unless viewer is Self or CourseMate
    string? Bio,
    string? ProfilePhotoUrl,
    string? CoverPhotoUrl,
    string? PhoneNumber,      // null unless viewer is Self or CourseMate
    string? LinkedInUrl,
    string? FacebookUrl,
    string? TwitterUrl,
    string? GitHubUrl,
    string? WebsiteUrl,
    string? Email,            // null unless viewer is Self or CourseMate
    string Role,
    List<UserEducationDto> Education,
    List<PublicCourseDto> Courses,    // teachers: always shown; students: only if viewer is Self or CourseMate
    string ViewerRelation     // "Self" | "CourseMate" | "Stranger"
);
