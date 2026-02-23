namespace EduNexis.Domain.Entities;

public class User : BaseEntity
{
    public string Email { get; private set; } = string.Empty;
    public string PasswordHash { get; private set; } = string.Empty;
    public string? RefreshToken { get; private set; }
    public DateTime? RefreshTokenExpiry { get; private set; }
    public UserRole Role { get; private set; }
    public bool IsActive { get; private set; } = true;
    public bool IsProfileComplete { get; private set; } = false;

    // Navigation
    public UserProfile? Profile { get; private set; }
    public ICollection<CourseMember> CourseMembers { get; private set; } = [];
    public ICollection<TeacherQuota> TeacherQuotas { get; private set; } = [];
    public ICollection<Notification> Notifications { get; private set; } = [];

    protected User() { }

    public static User Create(string email, string passwordHash, UserRole role)
    {
        ValidateEmailMatchesRole(email, role);
        return new User
        {
            Email = email.ToLowerInvariant(),
            PasswordHash = passwordHash,
            Role = role
        };
    }

    public void SetRefreshToken(string token, DateTime expiry)
    {
        RefreshToken = token;
        RefreshTokenExpiry = expiry;
        SetUpdatedAt();
    }

    public void ClearRefreshToken()
    {
        RefreshToken = null;
        RefreshTokenExpiry = null;
        SetUpdatedAt();
    }

    public bool IsRefreshTokenValid(string token) =>
        RefreshToken == token &&
        RefreshTokenExpiry.HasValue &&
        RefreshTokenExpiry.Value > DateTime.UtcNow;

    public void MarkProfileComplete() { IsProfileComplete = true; SetUpdatedAt(); }
    public void MarkProfileIncomplete() { IsProfileComplete = false; SetUpdatedAt(); }
    public void Deactivate() { IsActive = false; SetUpdatedAt(); }
    public void Activate() { IsActive = true; SetUpdatedAt(); }

    private static void ValidateEmailMatchesRole(string email, UserRole role)
    {
        if (role == UserRole.Teacher &&
            !email.EndsWith("@just.edu.bd", StringComparison.OrdinalIgnoreCase))
            throw new DomainException("Teacher email must end with @just.edu.bd");

        if (role == UserRole.Student &&
            !email.EndsWith("@student.just.edu.bd", StringComparison.OrdinalIgnoreCase))
            throw new DomainException("Student email must end with @student.just.edu.bd");
    }
}
