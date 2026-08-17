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
    string? Headline,
    string? ProfilePhotoUrl,
    string? CoverPhotoUrl,
    string? PhoneNumber,
    string? OfficeLocation,
    string? OfficeHours,
    string? ResearchInterestsCsv,
    string? FieldsOfWorkCsv,
    string? LinkedInUrl,
    string? FacebookUrl,
    string? TwitterUrl,
    string? GitHubUrl,
    string? WebsiteUrl,
    int ProfileCompletionPercent,
    bool IsPublicProfile,
    string? PublicSlug
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

public record UserPublicationDto(
    Guid Id,
    string Title,
    string Authors,
    string? Venue,
    int Year,
    string? Url,
    string Type,
    int OrderIndex,
    string? PdfUrl,
    long? PdfSizeBytes,
    DateTime? PdfUploadedAt,
    bool IsPdfPublic
);

public record PublicCourseDto(
    Guid Id,
    string Title,
    string CourseCode,
    string Department,
    string AcademicSession,
    string Semester,
    string CourseType,
    bool IsArchived
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
    string? StudentId,
    string? Bio,
    string? Headline,
    string? ProfilePhotoUrl,
    string? CoverPhotoUrl,
    string? PhoneNumber,
    string? OfficeLocation,
    string? OfficeHours,
    string? ResearchInterestsCsv,
    string? FieldsOfWorkCsv,
    string? LinkedInUrl,
    string? FacebookUrl,
    string? TwitterUrl,
    string? GitHubUrl,
    string? WebsiteUrl,
    string? Email,
    string Role,
    List<UserEducationDto> Education,
    List<UserPublicationDto> Publications,
    List<PublicCourseDto> Courses,
    int RunningCoursesCount,
    int ArchivedCoursesCount,
    string ViewerRelation
);