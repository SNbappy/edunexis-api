namespace EduNexis.Domain.Enums;

public enum CourseInvitationStatus
{
    Pending = 0,
    Accepted = 1,
    Declined = 2,
    /// <summary>Withdrawn by the sender before it was answered.</summary>
    Revoked = 3,
}
