namespace EduNexis.Domain.Entities;

public class Course : BaseEntity
{
    public string Title { get; private set; } = string.Empty;
    public string CourseCode { get; private set; } = string.Empty;
    public decimal CreditHours { get; private set; }
    public string Department { get; private set; } = string.Empty;
    public string AcademicSession { get; private set; } = string.Empty;
    public string Semester { get; private set; } = string.Empty;
    public string? Section { get; private set; }
    public CourseType CourseType { get; private set; }
    public string? Description { get; private set; }
    public string CoverImageUrl { get; private set; } = string.Empty;
    public string JoiningCode { get; private set; } = string.Empty;
    public Guid TeacherId { get; private set; }
    public bool IsArchived { get; private set; } = false;

    // Navigation
    public User Teacher { get; private set; } = null!;
    public ICollection<CourseMember> Members { get; private set; } = [];
    public ICollection<JoinRequest> JoinRequests { get; private set; } = [];
    // Remaining navigations added after Batch 2 entities exist

    protected Course() { }

    public static Course Create(
        string title, string courseCode, decimal creditHours,
        string department, string academicSession, string semester,
        string? section, CourseType courseType,
        string? description, string coverImageUrl, Guid teacherId)
    {
        if (string.IsNullOrWhiteSpace(title))
            throw new DomainException("Course title is required.");

        return new Course
        {
            Title = title,
            CourseCode = courseCode,
            CreditHours = creditHours,
            Department = department,
            AcademicSession = academicSession,
            Semester = semester,
            Section = section,
            CourseType = courseType,
            Description = description,
            CoverImageUrl = coverImageUrl,
            JoiningCode = GenerateJoiningCode(),
            TeacherId = teacherId
        };
    }

    public void Update(
        string title, string courseCode, decimal creditHours,
        string department, string academicSession, string semester,
        string? section, CourseType courseType, string? description)
    {
        Title = title;
        CourseCode = courseCode;
        CreditHours = creditHours;
        Department = department;
        AcademicSession = academicSession;
        Semester = semester;
        Section = section;
        CourseType = courseType;
        Description = description;
        SetUpdatedAt();
    }

    public void SetCoverImage(string url) { CoverImageUrl = url; SetUpdatedAt(); }
    public void Archive() { IsArchived = true; SetUpdatedAt(); }
    public void RegenerateJoiningCode() { JoiningCode = GenerateJoiningCode(); SetUpdatedAt(); }

    private static string GenerateJoiningCode() =>
        Guid.NewGuid().ToString("N")[..8].ToUpper();
}
