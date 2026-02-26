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
    string? PhoneNumber,
    string? LinkedInUrl,
    int ProfileCompletionPercent
);
