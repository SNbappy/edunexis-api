namespace EduNexis.Domain.Entities;

public class AttendanceSession : BaseEntity
{
    public Guid CourseId { get; private set; }
    public DateOnly Date { get; private set; }
    public string? Topic { get; private set; }
    public Guid CreatedById { get; private set; }

    // Navigation
    public Course Course { get; private set; } = null!;
    public User CreatedBy { get; private set; } = null!;
    public ICollection<AttendanceRecord> Records { get; private set; } = [];

    protected AttendanceSession() { }

    public static AttendanceSession Create(
        Guid courseId, DateOnly date, string? topic, Guid createdById) =>
        new()
        {
            CourseId = courseId,
            Date = date,
            Topic = topic,
            CreatedById = createdById
        };

    public void UpdateTopic(string? topic) { Topic = topic; SetUpdatedAt(); }
}
