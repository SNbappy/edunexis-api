namespace EduNexis.Application.DTOs;

public record CourseMemberDto(
    Guid UserId,
    string FullName,
    string Email,
    string? StudentId,
    string? ProfilePhotoUrl,
    bool IsCR,
    DateTime JoinedAt
);
