namespace EduNexis.Domain.Entities;

public class CTEvent : BaseEntity
{
    public Guid CourseId { get; private set; }
    public string Title { get; private set; } = string.Empty;
    public decimal MaxMarks { get; private set; }
    public Guid CreatedById { get; private set; }

    // Navigation
    public Course Course { get; private set; } = null!;
    public User CreatedBy { get; private set; } = null!;
    public ICollection<CTSubmission> Submissions { get; private set; } = [];

    protected CTEvent() { }

    public static CTEvent Create(
        Guid courseId, string title, decimal maxMarks, Guid createdById)
    {
        if (string.IsNullOrWhiteSpace(title))
            throw new DomainException("CT title is required.");
        if (maxMarks <= 0)
            throw new DomainException("Max marks must be greater than zero.");

        return new CTEvent
        {
            CourseId = courseId,
            Title = title,
            MaxMarks = maxMarks,
            CreatedById = createdById
        };
    }

    public void Update(string title, decimal maxMarks)
    {
        Title = title;
        MaxMarks = maxMarks;
        SetUpdatedAt();
    }
}
