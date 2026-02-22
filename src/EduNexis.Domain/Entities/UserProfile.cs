namespace EduNexis.Domain.Entities;

public class UserProfile : BaseEntity
{
    public Guid UserId { get; private set; }
    public string FullName { get; private set; } = string.Empty;
    public string Department { get; private set; } = string.Empty;
    public string? Designation { get; private set; }
    public string? StudentId { get; private set; }
    public string? Bio { get; private set; }
    public string? ProfilePhotoUrl { get; private set; }
    public string? PhoneNumber { get; private set; }
    public string? LinkedInUrl { get; private set; }
    public int ProfileCompletionPercent { get; private set; }

    // Navigation
    public User User { get; private set; } = null!;

    protected UserProfile() { }

    public static UserProfile Create(Guid userId) =>
        new() { UserId = userId };

    public void Update(
        string fullName, string department,
        string? designation, string? studentId,
        string? bio, string? phoneNumber, string? linkedInUrl)
    {
        FullName = fullName;
        Department = department;
        Designation = designation;
        StudentId = studentId;
        Bio = bio;
        PhoneNumber = phoneNumber;
        LinkedInUrl = linkedInUrl;
        SetUpdatedAt();
        RecalculateCompletion();
    }

    public void SetProfilePhoto(string url)
    {
        ProfilePhotoUrl = url;
        SetUpdatedAt();
        RecalculateCompletion();
    }

    public bool MeetsRequirement() =>
        !string.IsNullOrWhiteSpace(FullName) &&
        !string.IsNullOrWhiteSpace(Department) &&
        (!string.IsNullOrWhiteSpace(Designation) ||
         !string.IsNullOrWhiteSpace(StudentId));

    private void RecalculateCompletion()
    {
        var score = 0;
        if (!string.IsNullOrWhiteSpace(FullName)) score += 30;
        if (!string.IsNullOrWhiteSpace(Department)) score += 25;
        if (!string.IsNullOrWhiteSpace(Designation) ||
            !string.IsNullOrWhiteSpace(StudentId)) score += 25;
        if (!string.IsNullOrWhiteSpace(ProfilePhotoUrl)) score += 10;
        if (!string.IsNullOrWhiteSpace(Bio)) score += 5;
        if (!string.IsNullOrWhiteSpace(PhoneNumber)) score += 5;
        ProfileCompletionPercent = score;
    }
}
