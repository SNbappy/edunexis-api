namespace EduNexis.Domain.Entities;

/// <summary>
/// A teacher who shares a course with its owner.
///
/// Deliberately holds co-teachers only. <see cref="Course.TeacherId"/> stays
/// exactly what it always was — the one owner — so every existing owner-only
/// rule (delete, archive, restore, transfer) keeps working untouched. Adding a
/// second concept of ownership and rewriting sixty authorisation checks to use
/// it is how a permissions hole gets introduced; widening the checks that
/// should admit a colleague is a smaller, reversible change.
/// </summary>
public class CourseTeacher : BaseEntity
{
    public Guid CourseId { get; private set; }
    public Guid UserId { get; private set; }

    /// <summary>Who invited them, for the audit trail.</summary>
    public Guid AddedById { get; private set; }

    public DateTime AddedAt { get; private set; } = DateTime.UtcNow;

    // Navigation
    public Course Course { get; private set; } = null!;
    public User User { get; private set; } = null!;

    protected CourseTeacher() { }

    public static CourseTeacher Create(Guid courseId, Guid userId, Guid addedById) =>
        new()
        {
            Id = Guid.NewGuid(),
            CourseId = courseId,
            UserId = userId,
            AddedById = addedById,
            AddedAt = DateTime.UtcNow,
        };
}
