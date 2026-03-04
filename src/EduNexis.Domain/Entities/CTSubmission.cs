namespace EduNexis.Domain.Entities;

public class CTSubmission : BaseEntity
{
    public Guid CTEventId { get; private set; }
    public Guid StudentId { get; private set; }
    public decimal? ObtainedMarks { get; private set; }
    public bool IsAbsent { get; private set; } = false;
    public string? Remarks { get; private set; }
    public DateTime? MarkedAt { get; private set; }

    // Navigation
    public CTEvent CTEvent { get; private set; } = null!;
    public User Student { get; private set; } = null!;

    protected CTSubmission() { }

    public static CTSubmission Create(Guid ctEventId, Guid studentId) =>
        new() { CTEventId = ctEventId, StudentId = studentId };

    public void AssignMarks(decimal marks, string? remarks)
    {
        ObtainedMarks = marks;
        IsAbsent = false;
        Remarks = remarks;
        MarkedAt = DateTime.UtcNow;
        SetUpdatedAt();
    }

    public void MarkAbsent(string? remarks)
    {
        ObtainedMarks = null;
        IsAbsent = true;
        Remarks = remarks;
        MarkedAt = DateTime.UtcNow;
        SetUpdatedAt();
    }
}
