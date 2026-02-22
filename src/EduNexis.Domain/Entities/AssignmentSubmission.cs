namespace EduNexis.Domain.Entities;

public class AssignmentSubmission : BaseEntity
{
    public Guid AssignmentId { get; private set; }
    public Guid StudentId { get; private set; }
    public SubmissionType SubmissionType { get; private set; }
    public string? TextContent { get; private set; }
    public string? FileUrl { get; private set; }
    public string? LinkUrl { get; private set; }
    public DateTime SubmittedAt { get; private set; }
    public bool IsLate { get; private set; }
    public decimal? Marks { get; private set; }
    public string? Feedback { get; private set; }
    public bool IsGraded { get; private set; } = false;
    public DateTime? GradedAt { get; private set; }

    // Navigation
    public Assignment Assignment { get; private set; } = null!;
    public User Student { get; private set; } = null!;
    public PlagiarismReport? PlagiarismReport { get; private set; }
    public GradeComplaint? GradeComplaint { get; private set; }

    protected AssignmentSubmission() { }

    public static AssignmentSubmission Create(
        Guid assignmentId, Guid studentId,
        SubmissionType type, string? text,
        string? fileUrl, string? linkUrl, bool isLate)
    {
        return new AssignmentSubmission
        {
            AssignmentId = assignmentId,
            StudentId = studentId,
            SubmissionType = type,
            TextContent = text,
            FileUrl = fileUrl,
            LinkUrl = linkUrl,
            SubmittedAt = DateTime.UtcNow,
            IsLate = isLate
        };
    }

    public void Update(SubmissionType type, string? text, string? fileUrl, string? linkUrl)
    {
        SubmissionType = type;
        TextContent = text;
        FileUrl = fileUrl;
        LinkUrl = linkUrl;
        SubmittedAt = DateTime.UtcNow;
        SetUpdatedAt();
    }

    public void Grade(decimal marks, string? feedback)
    {
        Marks = marks;
        Feedback = feedback;
        IsGraded = true;
        GradedAt = DateTime.UtcNow;
        SetUpdatedAt();
    }
}
