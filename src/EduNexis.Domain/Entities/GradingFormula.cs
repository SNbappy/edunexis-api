namespace EduNexis.Domain.Entities;

public class GradingFormula : BaseEntity
{
    public Guid CourseId { get; private set; }
    public decimal TotalMarks { get; private set; }
    public bool IsPublished { get; private set; } = false;
    public DateTime? PublishedAt { get; private set; }

    // Navigation
    public Course Course { get; private set; } = null!;
    public ICollection<FormulaComponent> Components { get; private set; } = [];
    public ICollection<FinalMark> FinalMarks { get; private set; } = [];

    protected GradingFormula() { }

    public static GradingFormula Create(Guid courseId, decimal totalMarks) =>
        new() { CourseId = courseId, TotalMarks = totalMarks };

    public void UpdateTotalMarks(decimal totalMarks)
    {
        TotalMarks = totalMarks;
        SetUpdatedAt();
    }

    public void Publish()
    {
        IsPublished = true;
        PublishedAt = DateTime.UtcNow;
        SetUpdatedAt();
    }

    public void Unpublish()
    {
        IsPublished = false;
        PublishedAt = null;
        SetUpdatedAt();
    }
}
