namespace EduNexis.Domain.Enums;

/// <summary>
/// Stored by name (see AppDbContext), so these may be reordered freely — but
/// never renamed without migrating the existing Notifications and
/// NotificationPreferences rows that reference them as strings.
///
/// Every value here must also appear in the catalogue in
/// GetNotificationPreferencesQuery, otherwise the type becomes unmanageable
/// from Settings. A unit test enforces that.
/// </summary>
public enum NotificationType
{
    JoinRequestReceived,
    CourseJoinApproved,
    CourseJoinRejected,
    NewMaterial,
    NewAssignment,
    AssignmentDeadlineReminder,
    MarksPublished,
    GradeComplaint,
    NewAnnouncement,
    General,

    // --- Added so the things that actually happen in a course are reported ---

    /// <summary>A student's submission has been marked.</summary>
    AssignmentGraded,

    /// <summary>Deadline, marks or instructions changed on an existing assignment.</summary>
    AssignmentUpdated,

    /// <summary>An assignment was withdrawn.</summary>
    AssignmentRemoved,

    /// <summary>A student turned work in — for the teacher.</summary>
    SubmissionReceived,

    /// <summary>Someone replied under an announcement.</summary>
    NewComment,

    /// <summary>The register was taken for a session.</summary>
    AttendanceRecorded,

    /// <summary>A student left, or was removed from, the course — for the teacher.</summary>
    MemberLeft,

    /// <summary>The course was archived or restored.</summary>
    CourseArchived,
}
