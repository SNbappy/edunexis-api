namespace EduNexis.Domain.Entities;

public class JoinRequest : BaseEntity
{
    public Guid CourseId { get; private set; }
    public Guid StudentId { get; private set; }
    public JoinRequestStatus Status { get; private set; } = JoinRequestStatus.Pending;
    public Guid? ReviewedById { get; private set; }
    public DateTime? ReviewedAt { get; private set; }

    // Navigation
    public Course Course { get; private set; } = null!;
    public User Student { get; private set; } = null!;
    public User? ReviewedBy { get; private set; }

    protected JoinRequest() { }

    public static JoinRequest Create(Guid courseId, Guid studentId) =>
        new() { CourseId = courseId, StudentId = studentId };

    public void Approve(Guid reviewerId)
    {
        if (Status != JoinRequestStatus.Pending)
            throw new DomainException("Only pending requests can be approved.");
        Status = JoinRequestStatus.Approved;
        ReviewedById = reviewerId;
        ReviewedAt = DateTime.UtcNow;
        SetUpdatedAt();
    }

    public void Reject(Guid reviewerId)
    {
        if (Status != JoinRequestStatus.Pending)
            throw new DomainException("Only pending requests can be rejected.");
        Status = JoinRequestStatus.Rejected;
        ReviewedById = reviewerId;
        ReviewedAt = DateTime.UtcNow;
        SetUpdatedAt();
    }
}
