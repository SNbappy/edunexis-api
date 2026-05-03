namespace EduNexis.Domain.Entities;

public class UserProfile : BaseEntity
{
    public Guid UserId { get; private set; }
    public string FullName { get; private set; } = string.Empty;
    public string Department { get; private set; } = string.Empty;
    public string? Designation { get; private set; }
    public string? StudentId { get; private set; }
    public string? Bio { get; private set; }
    public string? Headline { get; private set; }
    public string? ProfilePhotoUrl { get; private set; }
    public string? CoverPhotoUrl { get; private set; }
    public string? PhoneNumber { get; private set; }
    public string? OfficeLocation { get; private set; }
    public string? OfficeHours { get; private set; }
    public string? ResearchInterestsCsv { get; private set; }
    public string? FieldsOfWorkCsv { get; private set; }
    public string? LinkedInUrl { get; private set; }
    public string? FacebookUrl { get; private set; }
    public string? TwitterUrl { get; private set; }
    public string? GitHubUrl { get; private set; }
    public string? WebsiteUrl { get; private set; }
    public int ProfileCompletionPercent { get; private set; }
    public bool IsPublicProfile { get; private set; } = false;
    public string? PublicSlug { get; private set; }

    public User User { get; private set; } = null!;

    protected UserProfile() { }

    public static UserProfile Create(Guid userId, string fullName = "") =>
        new()
        {
            UserId = userId,
            FullName = fullName,
            ProfileCompletionPercent = string.IsNullOrWhiteSpace(fullName) ? 0 : 30
        };

    public void Update(
        string fullName, string department,
        string? designation, string? studentId,
        string? bio, string? headline, string? phoneNumber,
        string? officeLocation, string? officeHours,
        string? researchInterestsCsv, string? fieldsOfWorkCsv,
        string? linkedInUrl, string? facebookUrl,
        string? twitterUrl, string? gitHubUrl, string? websiteUrl)
    {
        FullName = fullName;
        Department = department;
        Designation = designation;
        StudentId = studentId;
        Bio = bio;
        Headline = headline;
        PhoneNumber = phoneNumber;
        OfficeLocation = officeLocation;
        OfficeHours = officeHours;
        ResearchInterestsCsv = researchInterestsCsv;
        FieldsOfWorkCsv = fieldsOfWorkCsv;
        LinkedInUrl = linkedInUrl;
        FacebookUrl = facebookUrl;
        TwitterUrl = twitterUrl;
        GitHubUrl = gitHubUrl;
        WebsiteUrl = websiteUrl;
        SetUpdatedAt();
        RecalculateCompletion();
    }

    public void SetProfilePhoto(string url)
    {
        ProfilePhotoUrl = url;
        SetUpdatedAt();
        RecalculateCompletion();
    }

    public void RemoveProfilePhoto()
    {
        ProfilePhotoUrl = null;
        SetUpdatedAt();
        RecalculateCompletion();
    }

    public void SetCoverPhoto(string url)
    {
        CoverPhotoUrl = url;
        SetUpdatedAt();
    }

    public void RemoveCoverPhoto()
    {
        CoverPhotoUrl = null;
        SetUpdatedAt();
    }

    /// <summary>
    /// Role-aware completeness check.
    /// Teachers must have Designation; students must have StudentId.
    /// Both must have FullName + Department.
    /// </summary>
    public bool MeetsRequirement(UserRole role)
    {
        if (string.IsNullOrWhiteSpace(FullName)) return false;
        if (string.IsNullOrWhiteSpace(Department)) return false;
        if (role == UserRole.Teacher && string.IsNullOrWhiteSpace(Designation)) return false;
        if (role == UserRole.Student && string.IsNullOrWhiteSpace(StudentId)) return false;
        return true;
    }

    private void RecalculateCompletion()
    {
        var score = 0;
        if (!string.IsNullOrWhiteSpace(FullName)) score += 25;
        if (!string.IsNullOrWhiteSpace(Department)) score += 20;
        if (!string.IsNullOrWhiteSpace(Designation) || !string.IsNullOrWhiteSpace(StudentId)) score += 20;
        if (!string.IsNullOrWhiteSpace(ProfilePhotoUrl)) score += 10;
        if (!string.IsNullOrWhiteSpace(Bio)) score += 5;
        if (!string.IsNullOrWhiteSpace(Headline)) score += 5;
        if (!string.IsNullOrWhiteSpace(PhoneNumber)) score += 5;
        if (!string.IsNullOrWhiteSpace(LinkedInUrl) ||
            !string.IsNullOrWhiteSpace(GitHubUrl) ||
            !string.IsNullOrWhiteSpace(WebsiteUrl)) score += 5;
        if (!string.IsNullOrWhiteSpace(ResearchInterestsCsv) ||
            !string.IsNullOrWhiteSpace(FieldsOfWorkCsv)) score += 5;
        ProfileCompletionPercent = score;
    }

    /// <summary>
    /// Make this profile visible on the public faculty directory. Slug must
    /// be pre-validated (format + uniqueness) by the caller in the Application layer.
    /// </summary>
    public void MakePublic(string slug)
    {
        if (string.IsNullOrWhiteSpace(slug))
            throw new DomainException("Slug is required to make profile public.");
        IsPublicProfile = true;
        PublicSlug = slug;
        SetUpdatedAt();
    }

    public void MakePrivate()
    {
        IsPublicProfile = false;
        SetUpdatedAt();
    }

    public void UpdateSlug(string slug)
    {
        if (string.IsNullOrWhiteSpace(slug))
            throw new DomainException("Slug is required.");
        PublicSlug = slug;
        SetUpdatedAt();
    }
}