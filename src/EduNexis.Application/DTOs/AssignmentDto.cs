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
    DateTime CreatedAt,
    bool IsPublished = false,
    DateTime? PublishedAt = null,
    int TotalStudentsCount = 0,
    bool IsMarksComplete = false
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
    IReadOnlyList<SubmissionAttachmentDto>? Attachments = null,
    /// <summary>
    /// False while the student is still assembling a draft. Teachers never
    /// receive these at all; the student's own view uses it to choose between
    /// "Turn in" and "Unsubmit".
    /// </summary>
    bool IsTurnedIn = true,
    DateTime? TurnedInAt = null,
    /// <summary>A 0 awarded automatically because nothing was turned in.</summary>
    bool IsAutoZero = false,
    /// <summary>
    /// The student's profile photo. Without it every submission row fell back to
    /// initials even for students who had uploaded a picture, so the same person
    /// appeared as a photo on the roster and as two letters here.
    /// </summary>
    string? StudentPhotoUrl = null
);
