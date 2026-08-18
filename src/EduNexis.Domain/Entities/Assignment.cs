namespace EduNexis.Domain.Entities;

public class Assignment : BaseEntity
{
    public Guid CourseId { get; private set; }
    public string Title { get; private set; } = string.Empty;
    public string? Instructions { get; private set; }
    public DateTime Deadline { get; private set; }
    public bool AllowLateSubmission { get; private set; } = false;
    public decimal MaxMarks { get; private set; }
    public string? RubricNotes { get; private set; }
    public string? ReferenceFileUrl { get; private set; }
    public Guid CreatedById { get; private set; }

    /// <summary>
    /// Explicitly closed by the teacher, as distinct from merely past its
    /// deadline.
    ///
    /// The two are not the same thing: an assignment that still accepts late
    /// work is past due but not finished with. Closing is the point at which
    /// nothing more can arrive, and therefore the only point at which "turned
    /// in nothing" is a final fact worth recording as a zero.
    /// </summary>
    public bool IsClosed { get; private set; } = false;

    public DateTime? ClosedAt { get; private set; }

    // Navigation
    public Course Course { get; private set; } = null!;
    public User CreatedBy { get; private set; } = null!;
    public ICollection<AssignmentSubmission> Submissions { get; private set; } = [];

    protected Assignment() { }

    public static Assignment Create(
        Guid courseId, string title, string? instructions,
        DateTime deadline, bool allowLate, decimal maxMarks,
        string? rubricNotes, string? referenceFileUrl, Guid createdById)
    {
        if (string.IsNullOrWhiteSpace(title))
            throw new DomainException("Assignment title is required.");
        if (maxMarks <= 0)
            throw new DomainException("Max marks must be greater than zero.");
        if (deadline <= DateTime.UtcNow)
            throw new DomainException("Deadline must be in the future.");

        return new Assignment
        {
            CourseId = courseId,
            Title = title,
            Instructions = instructions,
            Deadline = deadline,
            AllowLateSubmission = allowLate,
            MaxMarks = maxMarks,
            RubricNotes = rubricNotes,
            ReferenceFileUrl = referenceFileUrl,
            CreatedById = createdById
        };
    }

    /// <summary>
    /// Whether work can still arrive. Closing always wins; otherwise it is open
    /// until the deadline, and beyond it when late submission is allowed.
    /// </summary>
    public bool IsOpen() =>
        !IsClosed && (DateTime.UtcNow <= Deadline || AllowLateSubmission);

    public void Close()
    {
        IsClosed = true;
        ClosedAt = DateTime.UtcNow;
        SetUpdatedAt();
    }

    public void Reopen()
    {
        IsClosed = false;
        ClosedAt = null;
        SetUpdatedAt();
    }

    public void Update(string title, string? instructions,
        DateTime deadline, bool allowLate, decimal maxMarks, string? rubricNotes,
        string? referenceFileUrl = null, bool updateReferenceFile = false)
    {
        Title = title;
        Instructions = instructions;
        Deadline = deadline;
        AllowLateSubmission = allowLate;
        MaxMarks = maxMarks;
        RubricNotes = rubricNotes;
        if (updateReferenceFile)
        {
            ReferenceFileUrl = referenceFileUrl;
        }
        SetUpdatedAt();
    }
}
