namespace EduNexis.Domain.Entities;

public class PresentationEvent : BaseEntity
{
    public Guid CourseId { get; private set; }
    public string Title { get; private set; } = string.Empty;
    public decimal MaxMarks { get; private set; }
    public Guid CreatedById { get; private set; }

    // Navigation
    public Course Course { get; private set; } = null!;
    public User CreatedBy { get; private set; } = null!;
    public ICollection<PresentationMark> Marks { get; private set; } = [];

    protected PresentationEvent() { }

    public static PresentationEvent Create(
        Guid courseId, string title, decimal maxMarks, Guid createdById)
    {
        if (string.IsNullOrWhiteSpace(title))
            throw new DomainException("Presentation title is required.");
        if (maxMarks <= 0)
            throw new DomainException("Max marks must be greater than zero.");

        return new PresentationEvent
        {
            CourseId = courseId,
            Title = title,
            MaxMarks = maxMarks,
            CreatedById = createdById
        };
    }
}
