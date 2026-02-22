namespace EduNexis.Domain.Entities;

public class AttendanceRecord : BaseEntity
{
    public Guid SessionId { get; private set; }
    public Guid StudentId { get; private set; }
    public AttendanceStatus Status { get; private set; }
    public DateTime MarkedAt { get; private set; } = DateTime.UtcNow;

    // Navigation
    public AttendanceSession Session { get; private set; } = null!;
    public User Student { get; private set; } = null!;

    protected AttendanceRecord() { }

    public static AttendanceRecord Create(
        Guid sessionId, Guid studentId, AttendanceStatus status) =>
        new()
        {
            SessionId = sessionId,
            StudentId = studentId,
            Status = status
        };

    public void UpdateStatus(AttendanceStatus status)
    {
        Status = status;
        MarkedAt = DateTime.UtcNow;
        SetUpdatedAt();
    }
}
