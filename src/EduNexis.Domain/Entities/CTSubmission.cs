namespace EduNexis.Domain.Entities;

public class CTSubmission : BaseEntity
{
    public Guid CTEventId { get; private set; }
    public Guid StudentId { get; private set; }
    public string? BestCopyUrl { get; private set; }
    public string? WorstCopyUrl { get; private set; }
    public string? AvgCopyUrl { get; private set; }
    public decimal? Marks { get; private set; }
    public DateTime? MarkedAt { get; private set; }

    // Navigation
    public CTEvent CTEvent { get; private set; } = null!;
    public User Student { get; private set; } = null!;

    protected CTSubmission() { }

    public static CTSubmission Create(Guid ctEventId, Guid studentId) =>
        new() { CTEventId = ctEventId, StudentId = studentId };

    public void UploadCopies(string? best, string? worst, string? avg)
    {
        BestCopyUrl = best;
        WorstCopyUrl = worst;
        AvgCopyUrl = avg;
        SetUpdatedAt();
    }

    public void AssignMarks(decimal marks)
    {
        Marks = marks;
        MarkedAt = DateTime.UtcNow;
        SetUpdatedAt();
    }
}
