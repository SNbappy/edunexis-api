namespace EduNexis.Domain.Entities;

public class CourseEnrollment
{
    public Guid Id { get; private set; }
    public Guid CourseId { get; private set; }
    public Course Course { get; private set; } = null!;
    public Guid StudentId { get; private set; }
    public DateTime EnrolledAt { get; private set; }
    public bool IsActive { get; private set; }

    private CourseEnrollment() { }

    public static CourseEnrollment Create(Guid courseId, Guid studentId)
    {
        return new CourseEnrollment
        {
            Id = Guid.NewGuid(),
            CourseId = courseId,
            StudentId = studentId,
            EnrolledAt = DateTime.UtcNow,
            IsActive = true
        };
    }

    public void Leave() => IsActive = false;
}
