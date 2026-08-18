namespace EduNexis.Domain.Entities;

/// <summary>
/// An invitation for another teacher to help run a course.
///
/// Nobody is added to a course without agreeing to it: accepting is what creates
/// the <see cref="CourseTeacher"/> row. An invitation that is never answered
/// simply stays pending and can be withdrawn by whoever sent it.
/// </summary>
public class CourseInvitation : BaseEntity
{
    public Guid CourseId { get; private set; }

    /// <summary>The teacher being invited.</summary>
    public Guid InvitedUserId { get; private set; }

    public Guid InvitedById { get; private set; }

    public CourseInvitationStatus Status { get; private set; } = CourseInvitationStatus.Pending;

    public DateTime? RespondedAt { get; private set; }

    /// <summary>Optional note from the sender.</summary>
    public string? Message { get; private set; }

    // Navigation
    public Course Course { get; private set; } = null!;

    protected CourseInvitation() { }

    public static CourseInvitation Create(
        Guid courseId, Guid invitedUserId, Guid invitedById, string? message) =>
        new()
        {
            Id = Guid.NewGuid(),
            CourseId = courseId,
            InvitedUserId = invitedUserId,
            InvitedById = invitedById,
            Message = string.IsNullOrWhiteSpace(message) ? null : message.Trim(),
            Status = CourseInvitationStatus.Pending,
        };

    public void Accept()
    {
        Status = CourseInvitationStatus.Accepted;
        RespondedAt = DateTime.UtcNow;
        SetUpdatedAt();
    }

    public void Decline()
    {
        Status = CourseInvitationStatus.Declined;
        RespondedAt = DateTime.UtcNow;
        SetUpdatedAt();
    }

    /// <summary>Withdrawn by the sender before it was answered.</summary>
    public void Revoke()
    {
        Status = CourseInvitationStatus.Revoked;
        RespondedAt = DateTime.UtcNow;
        SetUpdatedAt();
    }
}
