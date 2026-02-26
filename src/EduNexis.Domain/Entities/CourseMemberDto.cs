namespace EduNexis.Application.Courses.DTOs;

public class CourseMemberDto
{
    public Guid UserId { get; set; }
    public string FullName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string? StudentId { get; set; }
    public string? ProfilePhotoUrl { get; set; }
    public DateTime EnrolledAt { get; set; }
}
