namespace EduNexis.Domain.Entities;

public class GradeComplaintMessage : BaseEntity
{
    public Guid ComplaintId { get; private set; }
    public Guid SenderId { get; private set; }
    public string Message { get; private set; } = string.Empty;
    public DateTime SentAt { get; private set; } = DateTime.UtcNow;

    // Navigation
    public GradeComplaint Complaint { get; private set; } = null!;
    public User Sender { get; private set; } = null!;

    protected GradeComplaintMessage() { }

    public static GradeComplaintMessage Create(
        Guid complaintId, Guid senderId, string message)
    {
        if (string.IsNullOrWhiteSpace(message))
            throw new DomainException("Message cannot be empty.");

        return new GradeComplaintMessage
        {
            ComplaintId = complaintId,
            SenderId = senderId,
            Message = message
        };
    }
}
