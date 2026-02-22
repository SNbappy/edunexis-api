namespace EduNexis.Domain.Entities;

public class GradeComplaint : BaseEntity
{
    public Guid SubmissionId { get; private set; }
    public Guid StudentId { get; private set; }
    public string ComplaintText { get; private set; } = string.Empty;
    public ComplaintStatus Status { get; private set; } = ComplaintStatus.Open;

    // Navigation
    public AssignmentSubmission Submission { get; private set; } = null!;
    public User Student { get; private set; } = null!;
    public ICollection<GradeComplaintMessage> Messages { get; private set; } = [];

    protected GradeComplaint() { }

    public static GradeComplaint Create(
        Guid submissionId, Guid studentId, string complaintText)
    {
        if (string.IsNullOrWhiteSpace(complaintText))
            throw new DomainException("Complaint text is required.");

        return new GradeComplaint
        {
            SubmissionId = submissionId,
            StudentId = studentId,
            ComplaintText = complaintText
        };
    }

    public void MarkUnderReview() { Status = ComplaintStatus.UnderReview; SetUpdatedAt(); }
    public void Resolve() { Status = ComplaintStatus.Resolved; SetUpdatedAt(); }
}
