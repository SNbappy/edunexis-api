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

public record PublicProfileDto(
    Guid UserId,
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
    string Email,
    string Role,
    List<UserEducationDto> Education,
    List<PublicCourseDto> Courses
);
