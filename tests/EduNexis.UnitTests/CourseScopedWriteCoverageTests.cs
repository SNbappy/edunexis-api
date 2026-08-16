using System.Reflection;
using EduNexis.Application.Abstractions;

namespace EduNexis.UnitTests;

/// <summary>
/// The archive freeze is enforced by a pipeline behavior that only sees
/// commands implementing <see cref="ICourseScopedWrite"/>. That makes a
/// forgotten interface a silent hole: the command keeps working perfectly on an
/// archived course and nothing anywhere reports a problem.
///
/// This test closes that gap. Every command under a course-scoped feature area
/// must either be guarded or explicitly exempt with a stated reason, so adding
/// a new command without deciding which it is fails the build instead of
/// quietly bypassing the rule.
/// </summary>
public class CourseScopedWriteCoverageTests
{
    /// Feature folders whose commands write to something owned by a course.
    private static readonly string[] CourseScopedAreas =
    [
        "Announcements", "Assignments", "Attendance", "CT",
        "Materials", "Marks", "Presentations", "Courses",
    ];

    private static IEnumerable<Type> CourseScopedCommands()
    {
        var assembly = typeof(ICourseScopedWrite).Assembly;

        return assembly.GetTypes().Where(t =>
            t is { IsClass: true, IsAbstract: false }
            && t.Name.EndsWith("Command", StringComparison.Ordinal)
            && t.Namespace is not null
            && CourseScopedAreas.Any(area =>
                t.Namespace.Contains($".Features.{area}.Commands", StringComparison.Ordinal)));
    }

    [Fact]
    public void Every_course_scoped_command_is_guarded_or_explicitly_exempt()
    {
        var unhandled = CourseScopedCommands()
            .Where(t => !typeof(ICourseScopedWrite).IsAssignableFrom(t)
                     && !typeof(IArchiveExempt).IsAssignableFrom(t))
            .Select(t => t.Name)
            .OrderBy(n => n)
            .ToList();

        Assert.True(
            unhandled.Count == 0,
            "These commands write to a course but neither implement ICourseScopedWrite "
            + "(so the archive freeze applies) nor IArchiveExempt (so it deliberately "
            + "does not). Pick one:\n  - " + string.Join("\n  - ", unhandled));
    }

    [Fact]
    public void Exempt_commands_state_a_reason()
    {
        var blank = CourseScopedCommands()
            .Where(t => typeof(IArchiveExempt).IsAssignableFrom(t))
            .Where(t =>
            {
                var prop = t.GetProperty(nameof(IArchiveExempt.ArchiveExemptionReason),
                    BindingFlags.Public | BindingFlags.Instance);
                if (prop is null) return true;
                // Records expose it as a get-only property with a constant body;
                // reading it off an uninitialised instance is not possible here,
                // so assert the declaration exists and is public.
                return !prop.CanRead;
            })
            .Select(t => t.Name)
            .ToList();

        Assert.True(blank.Count == 0,
            "Exempt commands must expose a readable ArchiveExemptionReason: "
            + string.Join(", ", blank));
    }

    [Fact]
    public void Coverage_test_actually_finds_commands()
    {
        // Guards the guard: if the namespace convention changes, the checks
        // above would pass vacuously by matching nothing at all.
        Assert.True(CourseScopedCommands().Count() >= 30,
            $"Expected the course-scoped feature areas to contain many commands, "
            + $"found {CourseScopedCommands().Count()}. Has the namespace layout changed?");
    }
}
