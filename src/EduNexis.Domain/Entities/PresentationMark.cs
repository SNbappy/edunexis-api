namespace EduNexis.Domain.Entities;

public class PresentationMark : BaseEntity
{
    public Guid PresentationEventId { get; private set; }
    public Guid StudentId { get; private set; }
    public decimal Marks { get; private set; }
    public string? Feedback { get; private set; }
    public DateTime MarkedAt { get; private set; } = DateTime.UtcNow;

    // Navigation
    public PresentationEvent PresentationEvent { get; private set; } = null!;
    public User Student { get; private set; } = null!;

    protected PresentationMark() { }

    public static PresentationMark Create(
        Guid presentationEventId, Guid studentId, decimal marks, string? feedback) =>
        new()
        {
            PresentationEventId = presentationEventId,
            StudentId = studentId,
            Marks = marks,
            Feedback = feedback
        };

    public void Update(decimal marks, string? feedback)
    {
        Marks = marks;
        Feedback = feedback;
        MarkedAt = DateTime.UtcNow;
        SetUpdatedAt();
    }
}
