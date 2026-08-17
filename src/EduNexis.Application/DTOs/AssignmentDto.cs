namespace EduNexis.Application.DTOs;

public enum AssignmentMyStatus
{
    NotSubmitted,
    Submitted,
    Graded
}

public record AssignmentDto(
    Guid Id,
    Guid CourseId,
    string Title,
    string? Instructions,
    DateTime Deadline,
    bool AllowLateSubmission,
    decimal MaxMarks,
    string? RubricNotes,
    string? ReferenceFileUrl,
    bool IsOpen,
    int SubmissionCount,
    int GradedCount,
    AssignmentMyStatus? MyStatus,
    decimal? MyMarks,
    DateTime? MySubmittedAt,
    bool? MyIsLate,
    DateTime CreatedAt
);

public record SubmissionAttachmentDto(
    Guid Id,
    string Kind,
    string Url,
    string? FileName,
    long? FileSizeBytes
);

public record SubmissionDto(
    Guid Id,
    Guid AssignmentId,
    Guid StudentId,
    string StudentName,
    string SubmissionType,
    string? TextContent,
    /// <summary>First file, kept so older readers of this DTO still work.</summary>
    string? FileUrl,
    /// <summary>First link, kept for the same reason.</summary>
    string? LinkUrl,
    DateTime SubmittedAt,
    bool IsLate,
    decimal? Marks,
    string? Feedback,
    bool IsGraded,
    /// <summary>Everything turned in. Prefer this over FileUrl/LinkUrl.</summary>
    IReadOnlyList<SubmissionAttachmentDto>? Attachments = null
);
