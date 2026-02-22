namespace EduNexis.Domain.Entities;

public class PlagiarismReport : BaseEntity
{
    public Guid SubmissionId { get; private set; }
    public decimal SimilarityPercent { get; private set; }
    public bool IsAiGenerated { get; private set; }
    public string? ReportDetails { get; private set; }
    public DateTime CheckedAt { get; private set; } = DateTime.UtcNow;

    // Navigation
    public AssignmentSubmission Submission { get; private set; } = null!;

    protected PlagiarismReport() { }

    public static PlagiarismReport Create(
        Guid submissionId, decimal similarity,
        bool isAiGenerated, string? reportDetails) =>
        new()
        {
            SubmissionId = submissionId,
            SimilarityPercent = similarity,
            IsAiGenerated = isAiGenerated,
            ReportDetails = reportDetails
        };
}
