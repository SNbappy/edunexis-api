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
    public bool IsDeletedByOwner { get; private set; } = false;
    public DateTime? DeletedByOwnerAt { get; private set; }

    // Navigation
    public User Teacher { get; private set; } = null!;
    public ICollection<CourseMember> Members { get; private set; } = [];
    public ICollection<JoinRequest> JoinRequests { get; private set; } = [];
    public ICollection<AttendanceSession> AttendanceSessions { get; private set; } = [];
    public ICollection<Material> Materials { get; private set; } = [];
    public ICollection<Assignment> Assignments { get; private set; } = [];
    public ICollection<CTEvent> CTEvents { get; private set; } = [];
    public ICollection<PresentationEvent> PresentationEvents { get; private set; } = [];
    public ICollection<Announcement> Announcements { get; private set; } = [];
    public GradingFormula? GradingFormula { get; private set; }

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

    public void Archive()
    {
        if (IsArchived) throw new DomainException("Course is already archived.");
        IsArchived = true;
        SetUpdatedAt();
    }

    public void Unarchive()
    {
        if (!IsArchived) throw new DomainException("Course is not archived.");
        IsArchived = false;
        SetUpdatedAt();
    }

    /// <summary>
    /// Owner-initiated soft delete. Course moves to the teacher's "Recently
    /// deleted" list and can be restored within 30 days via RestoreByOwner().
    /// After the window elapses it becomes eligible for permanent purge.
    /// </summary>
    public void SoftDeleteByOwner()
    {
        if (IsDeletedByOwner) throw new DomainException("Course is already deleted.");
        IsDeletedByOwner = true;
        DeletedByOwnerAt = DateTime.UtcNow;
        SetUpdatedAt();
    }

    public void RestoreByOwner()
    {
        if (!IsDeletedByOwner) throw new DomainException("Course is not deleted.");
        if (DeletedByOwnerAt is null || DateTime.UtcNow > DeletedByOwnerAt.Value.AddDays(30))
            throw new DomainException("The 30-day restore window has expired.");
        IsDeletedByOwner = false;
        DeletedByOwnerAt = null;
        SetUpdatedAt();
    }

    /// <summary>True once the 30-day restore window has elapsed. Callers use
    /// this to decide whether a soft-deleted course is eligible for purge.</summary>
    public bool IsPastRestoreWindow =>
        IsDeletedByOwner && DeletedByOwnerAt is not null &&
        DateTime.UtcNow > DeletedByOwnerAt.Value.AddDays(30);

    public void RegenerateJoiningCode() { JoiningCode = GenerateJoiningCode(); SetUpdatedAt(); }

    private static string GenerateJoiningCode() =>
        Guid.NewGuid().ToString("N")[..8].ToUpper();
}
