namespace EduNexis.Domain.Entities;

public class CourseMember : BaseEntity
{
    public Guid CourseId { get; private set; }
    public Guid UserId { get; private set; }
    public bool IsCR { get; private set; } = false;
    public bool IsActive { get; private set; } = true;
    public DateTime JoinedAt { get; private set; } = DateTime.UtcNow;

    // Navigation
    public Course Course { get; private set; } = null!;
    public User User { get; private set; } = null!;

    protected CourseMember() { }

    public static CourseMember Create(Guid courseId, Guid userId) =>
        new() { CourseId = courseId, UserId = userId };

    public void PromoteToCR() { IsCR = true; SetUpdatedAt(); }
    public void DemoteFromCR() { IsCR = false; SetUpdatedAt(); }
    public void Remove() { IsActive = false; SetUpdatedAt(); }
}
