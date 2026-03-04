namespace EduNexis.Domain.Entities;

public class PresentationEvent : BaseEntity
{
    public Guid CourseId { get; private set; }
    public string Title { get; private set; } = string.Empty;
    public string? Description { get; private set; }
    public decimal MaxMarks { get; private set; }
    public DateTime? ScheduledDate { get; private set; }
    public PresentationStatus Status { get; private set; } = PresentationStatus.Scheduled;
    public PresentationFormat Format { get; private set; } = PresentationFormat.Individual;
    public string? Venue { get; private set; }
    public bool TopicsAllowed { get; private set; }
    public int? DurationPerGroupMinutes { get; private set; }
    public Guid CreatedById { get; private set; }

    public Course Course { get; private set; } = null!;
    public User CreatedBy { get; private set; } = null!;
    public ICollection<PresentationMark> Marks { get; private set; } = [];

    protected PresentationEvent() { }

    public static PresentationEvent Create(
        Guid courseId, string title, string? description, decimal maxMarks,
        DateTime? scheduledDate, PresentationFormat format, string? venue,
        bool topicsAllowed, int? durationPerGroupMinutes, Guid createdById)
    {
        if (string.IsNullOrWhiteSpace(title))
            throw new DomainException("Presentation title is required.");
        if (maxMarks <= 0)
            throw new DomainException("Max marks must be greater than zero.");

        return new PresentationEvent
        {
            CourseId = courseId,
            Title = title,
            Description = description,
            MaxMarks = maxMarks,
            ScheduledDate = scheduledDate,
            Status = PresentationStatus.Scheduled,
            Format = format,
            Venue = venue,
            TopicsAllowed = topicsAllowed,
            DurationPerGroupMinutes = durationPerGroupMinutes,
            CreatedById = createdById
        };
    }

    public void UpdateStatus(PresentationStatus status)
    {
        Status = status;
        SetUpdatedAt();
    }
}
