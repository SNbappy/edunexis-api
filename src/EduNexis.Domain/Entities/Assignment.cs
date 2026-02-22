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

    public bool IsOpen() => DateTime.UtcNow <= Deadline;

    public void Update(string title, string? instructions,
        DateTime deadline, bool allowLate, decimal maxMarks, string? rubricNotes)
    {
        Title = title;
        Instructions = instructions;
        Deadline = deadline;
        AllowLateSubmission = allowLate;
        MaxMarks = maxMarks;
        RubricNotes = rubricNotes;
        SetUpdatedAt();
    }
}
