namespace EduNexis.Application.Abstractions;

/// <summary>
/// Marks a command that changes something belonging to a course.
///
/// Implementing this is how a command opts into the archive freeze: the
/// <c>ArchivedCourseGuardBehavior</c> resolves the owning course before the
/// handler runs and refuses the write if that course is archived.
///
/// The check lives in one pipeline step rather than being repeated in ~35
/// handlers, because an archive rule that has to be remembered separately in
/// every new command is an archive rule that will eventually have a hole in it.
/// <c>CourseScopedWriteCoverageTests</c> asserts that every command under a
/// course-scoped feature folder implements this, so forgetting it fails the
/// build rather than silently letting writes through.
/// </summary>
public interface ICourseScopedWrite
{
    /// <summary>
    /// The course this command writes to, or null when there is nothing to
    /// guard (the target no longer exists — let the handler produce its own
    /// "not found", which is a better message than "archived").
    ///
    /// Commands that already carry a CourseId return it directly. Commands
    /// that carry only a child id (an assignment, a CT event, a submission)
    /// look up the parent here.
    /// </summary>
    ValueTask<Guid?> ResolveCourseIdAsync(IUnitOfWork uow, CancellationToken ct);
}

/// <summary>
/// Opts a command *out* of the archive freeze, with a stated reason.
///
/// Applied to the handful of operations that must keep working on an archived
/// course — unarchiving it, deleting it outright, and a student leaving. These
/// act on the course's own lifecycle rather than on its contents.
/// </summary>
public interface IArchiveExempt
{
    /// <summary>Why this write is allowed while archived. Documentation only.</summary>
    string ArchiveExemptionReason { get; }
}
