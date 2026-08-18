using EduNexis.Domain.Entities;

namespace EduNexis.Application.Abstractions;

/// <summary>
/// One place that answers "may this person act as a teacher on this course?".
///
/// Courses can now be shared. Rather than replace the sixty
/// <c>course.TeacherId == userId</c> checks scattered through the handlers —
/// which is how half of them end up trusting a new model and half the old one —
/// the owner check stays exactly as it is and the ones that should admit a
/// colleague call in here instead.
///
/// The split is deliberate:
///
///   <see cref="IsTeacherAsync"/>  everything about running the course —
///                                 attendance, materials, assignments, marks,
///                                 admitting students, inviting colleagues.
///
///   owner only                    archiving, deleting, restoring and handing
///                                 the course over. A co-teacher helps run a
///                                 course; they do not get to dispose of one
///                                 somebody else created.
/// </summary>
public static class CourseAccess
{
    /// <summary>
    /// True for the course owner or any accepted co-teacher.
    ///
    /// Accepts a null course so call sites that look one up and check
    /// permissions in the same breath do not each need their own guard; a course
    /// that does not exist has no teachers.
    /// </summary>
    public static async Task<bool> IsTeacherAsync(
        IUnitOfWork uow, Course? course, Guid userId, CancellationToken ct)
    {
        if (course is null) return false;
        if (course.TeacherId == userId) return true;
        return await IsCoTeacherAsync(uow, course.Id, userId, ct);
    }

    /// <summary>True for the course owner or any accepted co-teacher.</summary>
    public static async Task<bool> IsTeacherAsync(
        IUnitOfWork uow, Guid courseId, Guid userId, CancellationToken ct)
    {
        var course = await uow.Courses.GetByIdAsync(courseId, ct);
        if (course is null) return false;
        return await IsTeacherAsync(uow, course, userId, ct);
    }

    /// <summary>True only for an accepted co-teacher, not the owner.</summary>
    public static async Task<bool> IsCoTeacherAsync(
        IUnitOfWork uow, Guid courseId, Guid userId, CancellationToken ct)
    {
        var row = await uow.GetRepository<CourseTeacher>()
            .FirstOrDefaultAsync(t => t.CourseId == courseId && t.UserId == userId, ct);
        return row is not null;
    }

    /// <summary>Owner only. Use for anything that disposes of the course.</summary>
    public static bool IsOwner(Course course, Guid userId) => course.TeacherId == userId;

    /// <summary>Every teacher on the course, owner first.</summary>
    public static async Task<List<Guid>> TeacherIdsAsync(
        IUnitOfWork uow, Course course, CancellationToken ct)
    {
        var co = (await uow.GetRepository<CourseTeacher>()
                .FindAsync(t => t.CourseId == course.Id, ct))
            .Select(t => t.UserId);

        return new[] { course.TeacherId }.Concat(co).Distinct().ToList();
    }
}
