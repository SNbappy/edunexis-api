namespace EduNexis.Domain.Entities;

/// <summary>
/// One file or link attached to a submission.
///
/// A submission used to hold a single FileUrl and a single LinkUrl, so a task
/// answered with a report plus its source code plus a demo video could not be
/// turned in as one piece of work — students uploaded the first file, then
/// re-submitted to replace it, or zipped everything and lost the ability for a
/// teacher to open any one part.
///
/// The legacy FileUrl/LinkUrl columns are still written with the first file and
/// first link so anything reading the old shape keeps working; this table is
/// the complete list.
/// </summary>
public class SubmissionAttachment : BaseEntity
{
    public Guid SubmissionId { get; private set; }

    /// <summary>File or Link — decides whether Url is downloaded or opened.</summary>
    public SubmissionAttachmentKind Kind { get; private set; }

    public string Url { get; private set; } = string.Empty;

    /// <summary>Original filename for a file; null for a link.</summary>
    public string? FileName { get; private set; }

    public long? FileSizeBytes { get; private set; }

    /// <summary>Preserves the order the student added them in.</summary>
    public int SortOrder { get; private set; }

    public AssignmentSubmission Submission { get; private set; } = null!;

    protected SubmissionAttachment() { }

    public static SubmissionAttachment Create(
        Guid submissionId, SubmissionAttachmentKind kind,
        string url, string? fileName, long? fileSizeBytes, int sortOrder)
    {
        if (string.IsNullOrWhiteSpace(url))
            throw new DomainException("Attachment URL is required.");

        return new SubmissionAttachment
        {
            SubmissionId = submissionId,
            Kind = kind,
            Url = url,
            FileName = fileName,
            FileSizeBytes = fileSizeBytes,
            SortOrder = sortOrder,
        };
    }
}
