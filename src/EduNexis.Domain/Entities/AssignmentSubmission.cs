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

    /// <summary>
    /// Whether the student has actually handed this in.
    ///
    /// Attaching work and turning it in are two different acts. A student who
    /// uploads a draft an hour before the deadline and keeps editing has not
    /// submitted anything yet, and a teacher must not be able to read — or
    /// worse, mark — a half-finished answer. Nothing is visible to staff until
    /// this is true.
    /// </summary>
    public bool IsTurnedIn { get; private set; } = true;

    public DateTime? TurnedInAt { get; private set; }

    /// <summary>
    /// Set when the deadline passed with nothing turned in and the course
    /// awarded an automatic zero, so a real 0 can be told apart from one the
    /// teacher typed — and undone if the student is later excused.
    /// </summary>
    public bool IsAutoZero { get; private set; } = false;

    // Navigation
    public Assignment Assignment { get; private set; } = null!;
    public User Student { get; private set; } = null!;
    public PlagiarismReport? PlagiarismReport { get; private set; }
    public GradeComplaint? GradeComplaint { get; private set; }

    /// <summary>
    /// Every file and link turned in. FileUrl/LinkUrl above mirror the first of
    /// each so older readers keep working; this is the complete set.
    /// </summary>
    public ICollection<SubmissionAttachment> Attachments { get; private set; } = [];

    protected AssignmentSubmission() { }

    public static AssignmentSubmission Create(
        Guid assignmentId, Guid studentId,
        SubmissionType type, string? text,
        string? fileUrl, string? linkUrl, bool isLate,
        bool isTurnedIn = true)
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
            IsLate = isLate,
            IsTurnedIn = isTurnedIn,
            TurnedInAt = isTurnedIn ? DateTime.UtcNow : null,
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

    /// <summary>Hands the work in. <paramref name="isLate"/> is recomputed here
    /// because lateness is decided by when it was turned in, not when the first
    /// draft was attached.</summary>
    public void TurnIn(bool isLate)
    {
        IsTurnedIn = true;
        TurnedInAt = DateTime.UtcNow;
        IsLate = isLate;
        SetUpdatedAt();
    }

    /// <summary>Takes it back for more work. Any mark is cleared with it — a
    /// grade against withdrawn work would be a grade against nothing.</summary>
    public void Unsubmit()
    {
        IsTurnedIn = false;
        TurnedInAt = null;
        Marks = null;
        Feedback = null;
        IsGraded = false;
        GradedAt = null;
        SetUpdatedAt();
    }

    public void Grade(decimal marks, string? feedback)
    {
        Marks = marks;
        Feedback = feedback;
        IsGraded = true;
        GradedAt = DateTime.UtcNow;
        IsAutoZero = false;
        SetUpdatedAt();
    }

    /// <summary>The automatic 0 for a student who turned in nothing before the
    /// assignment closed.</summary>
    public static AssignmentSubmission CreateAutoZero(Guid assignmentId, Guid studentId)
        => new()
        {
            AssignmentId = assignmentId,
            StudentId = studentId,
            SubmissionType = SubmissionType.Text,
            SubmittedAt = DateTime.UtcNow,
            IsLate = true,
            IsTurnedIn = true,
            TurnedInAt = DateTime.UtcNow,
            Marks = 0,
            Feedback = "No submission received before the assignment closed.",
            IsGraded = true,
            GradedAt = DateTime.UtcNow,
            IsAutoZero = true,
        };
}
