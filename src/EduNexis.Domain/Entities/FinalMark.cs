namespace EduNexis.Domain.Entities;

public class FinalMark : BaseEntity
{
    public Guid FormulaId { get; private set; }
    public Guid CourseId { get; private set; }
    public Guid StudentId { get; private set; }
    public string BreakdownJson { get; private set; } = "{}";
    public decimal FinalMarkValue { get; private set; }
    public bool IsPublished { get; private set; } = false;
    public DateTime? PublishedAt { get; private set; }

    // Navigation
    public GradingFormula Formula { get; private set; } = null!;
    public Course Course { get; private set; } = null!;
    public User Student { get; private set; } = null!;

    protected FinalMark() { }

    public static FinalMark Create(
        Guid formulaId, Guid courseId,
        Guid studentId, string breakdownJson, decimal finalMark) =>
        new()
        {
            FormulaId = formulaId,
            CourseId = courseId,
            StudentId = studentId,
            BreakdownJson = breakdownJson,
            FinalMarkValue = finalMark
        };

    public void Publish()
    {
        IsPublished = true;
        PublishedAt = DateTime.UtcNow;
        SetUpdatedAt();
    }
}
