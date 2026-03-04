namespace EduNexis.Domain.Entities;

public class PresentationMark : BaseEntity
{
    public Guid PresentationEventId { get; private set; }
    public Guid StudentId { get; private set; }
    public decimal Marks { get; private set; }
    public bool IsAbsent { get; private set; }
    public string? Topic { get; private set; }
    public string? Feedback { get; private set; }
    public DateTime MarkedAt { get; private set; } = DateTime.UtcNow;

    public PresentationEvent PresentationEvent { get; private set; } = null!;
    public User Student { get; private set; } = null!;

    protected PresentationMark() { }

    public static PresentationMark Create(
        Guid presentationEventId, Guid studentId, decimal marks, bool isAbsent,
        string? topic, string? feedback) =>
        new()
        {
            PresentationEventId = presentationEventId,
            StudentId = studentId,
            Marks = marks,
            IsAbsent = isAbsent,
            Topic = topic,
            Feedback = feedback
        };

    public void Update(decimal marks, bool isAbsent, string? topic, string? feedback)
    {
        Marks = marks;
        IsAbsent = isAbsent;
        Topic = topic;
        Feedback = feedback;
        MarkedAt = DateTime.UtcNow;
        SetUpdatedAt();
    }
}
