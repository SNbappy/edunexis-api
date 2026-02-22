namespace EduNexis.Domain.Entities;

public class User : BaseEntity
{
    public string FirebaseUid { get; private set; } = string.Empty;
    public string Email { get; private set; } = string.Empty;
    public UserRole Role { get; private set; }
    public bool IsActive { get; private set; } = true;
    public bool IsProfileComplete { get; private set; } = false;

    // Navigation
    public UserProfile? Profile { get; private set; }
    public ICollection<CourseMember> CourseMembers { get; private set; } = [];
    public ICollection<TeacherQuota> TeacherQuotas { get; private set; } = [];
    // Notifications navigation added after Notification entity exists

    protected User() { }

    public static User Create(string firebaseUid, string email, UserRole role)
    {
        ValidateEmailMatchesRole(email, role);
        return new User
        {
            FirebaseUid = firebaseUid,
            Email = email.ToLowerInvariant(),
            Role = role
        };
    }

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
