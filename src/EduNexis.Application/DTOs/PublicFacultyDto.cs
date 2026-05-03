namespace EduNexis.Application.DTOs;

/// <summary>
/// Lightweight DTO for the faculty directory grid. No personal contact info.
/// </summary>
public record PublicFacultyCardDto(
    string Slug,
    string FullName,
    string Department,
    string? Designation,
    string? Headline,
    string? ProfilePhotoUrl,
    int CoursesTaught
);

/// <summary>
/// Full DTO for the public faculty profile page.
/// Excludes Email, PhoneNumber (private), StudentId (irrelevant for teachers).
/// </summary>
public record PublicFacultyProfileDto(
    string Slug,
    string FullName,
    string Department,
    string? Designation,
    string? Bio,
    string? Headline,
    string? ProfilePhotoUrl,
    string? CoverPhotoUrl,
    string? OfficeLocation,
    string? OfficeHours,
    string? ResearchInterestsCsv,
    string? FieldsOfWorkCsv,
    string? LinkedInUrl,
    string? FacebookUrl,
    string? TwitterUrl,
    string? GitHubUrl,
    string? WebsiteUrl,
    List<UserEducationDto> Education,
    List<UserPublicationDto> Publications,
    List<PublicCourseDto> Courses,
    int CoursesTaught
);

/// <summary>
/// Site-wide stats shown on the homepage hero.
/// </summary>
public record PublicStatsDto(
    int TeacherCount,
    int StudentCount,
    int CourseCount,
    int AssignmentCount
);