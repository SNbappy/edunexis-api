namespace EduNexis.Application.DTOs;

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
    DateTime CreatedAt
);

public record SubmissionDto(
    Guid Id,
    Guid AssignmentId,
    Guid StudentId,
    string StudentName,
    string SubmissionType,
    string? TextContent,
    string? FileUrl,
    string? LinkUrl,
    DateTime SubmittedAt,
    bool IsLate,
    decimal? Marks,
    string? Feedback,
    bool IsGraded
);
