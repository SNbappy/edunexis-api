using Microsoft.Extensions.Logging;

namespace EduNexis.Application.Features.Marks.Commands;

public record CalculateFinalMarksCommand(
    Guid CourseId,
    Guid TeacherId
) : ICommand<ApiResponse>;

public sealed class CalculateFinalMarksCommandHandler(
    IUnitOfWork uow,
    ILogger<CalculateFinalMarksCommandHandler> logger
) : ICommandHandler<CalculateFinalMarksCommand, ApiResponse>
{
    public async ValueTask<ApiResponse> Handle(
        CalculateFinalMarksCommand command, CancellationToken ct)
    {
        var course = await uow.Courses.GetByIdAsync(command.CourseId, ct)
            ?? throw new NotFoundException("Course", command.CourseId);

        if (course.TeacherId != command.TeacherId)
            throw new UnauthorizedException("Only the teacher can calculate final marks.");

        var formula = await uow.GetRepository<GradingFormula>()
            .FirstOrDefaultAsync(f => f.CourseId == command.CourseId, ct)
            ?? throw new NotFoundException("GradingFormula", command.CourseId);

        var components = (await uow.GetRepository<FormulaComponent>()
            .FindAsync(c => c.FormulaId == formula.Id, ct)).ToList();

        if (components.Count == 0)
            return ApiResponse.Fail("No formula components configured. Save a formula first.");

        // Filter: only active students, exclude the teacher (just in case schema includes them).
        var allMembers = await uow.GetRepository<CourseMember>()
            .FindAsync(m => m.CourseId == command.CourseId, ct);

        var students = allMembers
            .Where(m => m.IsActive && m.UserId != command.TeacherId)
            .ToList();

        if (students.Count == 0)
            return ApiResponse.Fail("No active students enrolled in this course.");

        int succeeded = 0;
        int failed = 0;

        foreach (var student in students)
        {
            try
            {
                decimal finalMark = 0m;
                var breakdown = new Dictionary<string, object>();

                foreach (var comp in components)
                {
                    decimal earned = comp.ComponentType switch
                    {
                        FormulaComponentType.CT => await CalculateCT(
                            command.CourseId, student.UserId, comp.SelectionRule, comp.MaxMarks, ct),

                        FormulaComponentType.Assignment => await CalculateAssignment(
                            command.CourseId, student.UserId, comp.SelectionRule, comp.MaxMarks, ct),

                        FormulaComponentType.Presentation => await CalculatePresentation(
                            command.CourseId, student.UserId, comp.SelectionRule, comp.MaxMarks, ct),

                        FormulaComponentType.Attendance => await CalculateAttendance(
                            command.CourseId, student.UserId, comp.MaxMarks, ct),

                        _ => 0m
                    };

                    breakdown[comp.ComponentType.ToString()] = new
                    {
                        rule = comp.SelectionRule,
                        earned,
                        maxMarks = comp.MaxMarks
                    };

                    finalMark += earned;
                }

                var breakdownJson = System.Text.Json.JsonSerializer.Serialize(breakdown);

                var existing = await uow.GetRepository<FinalMark>()
                    .FirstOrDefaultAsync(fm =>
                        fm.CourseId == command.CourseId &&
                        fm.StudentId == student.UserId, ct);

                if (existing is null)
                {
                    var fm = FinalMark.Create(
                        formula.Id, command.CourseId,
                        student.UserId, breakdownJson, finalMark);
                    await uow.GetRepository<FinalMark>().AddAsync(fm, ct);
                }
                else
                {
                    existing.UpdateMark(breakdownJson, finalMark);
                }

                succeeded++;
            }
            catch (Exception ex)
            {
                failed++;
                logger.LogError(ex,
                    "Failed to calculate final mark for student {StudentId} in course {CourseId}",
                    student.UserId, command.CourseId);
            }
        }

        try
        {
            await uow.SaveChangesAsync(ct);
        }
        catch (Exception ex)
        {
            logger.LogError(ex,
                "Failed to save calculated marks for course {CourseId}", command.CourseId);
            return ApiResponse.Fail("Could not save calculated marks. Check server logs.");
        }

        if (failed > 0 && succeeded == 0)
            return ApiResponse.Fail($"Calculation failed for all {failed} students. Check server logs.");

        if (failed > 0)
            return ApiResponse.Ok(
                $"Final marks calculated for {succeeded} students. {failed} failed (see logs).");

        return ApiResponse.Ok($"Final marks calculated for {succeeded} students.");
    }

    private async Task<decimal> CalculateCT(
        Guid courseId, Guid studentId, string rule, decimal maxMarks, CancellationToken ct)
    {
        if (maxMarks <= 0) return 0m;

        var ctEvents = (await uow.GetRepository<CTEvent>()
            .FindAsync(e => e.CourseId == courseId, ct)).ToList();

        if (ctEvents.Count == 0) return 0m;

        var marks = new List<decimal>();
        foreach (var ev in ctEvents)
        {
            if (ev.MaxMarks <= 0) continue;

            var sub = await uow.GetRepository<CTSubmission>()
                .FirstOrDefaultAsync(s =>
                    s.CTEventId == ev.Id && s.StudentId == studentId, ct);

            if (sub is null) continue;

            var scaled = sub.IsAbsent || sub.ObtainedMarks is null
                ? 0m
                : sub.ObtainedMarks.Value / ev.MaxMarks * maxMarks;
            marks.Add(Math.Round(scaled, 4));
        }

        return ApplyRule(marks, rule, maxMarks);
    }

    private async Task<decimal> CalculateAssignment(
        Guid courseId, Guid studentId, string rule, decimal maxMarks, CancellationToken ct)
    {
        if (maxMarks <= 0) return 0m;

        var assignments = (await uow.GetRepository<Assignment>()
            .FindAsync(a => a.CourseId == courseId, ct)).ToList();

        if (assignments.Count == 0) return 0m;

        var marks = new List<decimal>();
        foreach (var a in assignments)
        {
            if (a.MaxMarks <= 0) continue;

            var sub = await uow.GetRepository<AssignmentSubmission>()
                .FirstOrDefaultAsync(s =>
                    s.AssignmentId == a.Id && s.StudentId == studentId, ct);

            if (sub?.Marks is null) continue;

            var scaled = sub.Marks.Value / a.MaxMarks * maxMarks;
            marks.Add(Math.Round(scaled, 4));
        }

        return ApplyRule(marks, rule, maxMarks);
    }

    private async Task<decimal> CalculatePresentation(
        Guid courseId, Guid studentId, string rule, decimal maxMarks, CancellationToken ct)
    {
        if (maxMarks <= 0) return 0m;

        var events = (await uow.GetRepository<PresentationEvent>()
            .FindAsync(e => e.CourseId == courseId, ct)).ToList();

        if (events.Count == 0) return 0m;

        var marks = new List<decimal>();
        foreach (var ev in events)
        {
            if (ev.MaxMarks <= 0) continue;

            var mark = await uow.GetRepository<PresentationMark>()
                .FirstOrDefaultAsync(m =>
                    m.PresentationEventId == ev.Id && m.StudentId == studentId, ct);

            if (mark is null) continue;

            var scaled = mark.IsAbsent
                ? 0m
                : mark.Marks / ev.MaxMarks * maxMarks;
            marks.Add(Math.Round(scaled, 4));
        }

        return ApplyRule(marks, rule, maxMarks);
    }

    private async Task<decimal> CalculateAttendance(
        Guid courseId, Guid studentId, decimal maxMarks, CancellationToken ct)
    {
        if (maxMarks <= 0) return 0m;

        var sessions = (await uow.GetRepository<AttendanceSession>()
            .FindAsync(s => s.CourseId == courseId, ct)).ToList();

        if (sessions.Count == 0) return 0m;

        int present = 0;
        foreach (var session in sessions)
        {
            var record = await uow.GetRepository<AttendanceRecord>()
                .FirstOrDefaultAsync(r =>
                    r.SessionId == session.Id && r.StudentId == studentId, ct);

            if (record?.Status == AttendanceStatus.Present)
                present++;
        }

        decimal percentage = (decimal)present / sessions.Count;
        return Math.Round(percentage * maxMarks, 2);
    }

    private static decimal ApplyRule(List<decimal> marks, string rule, decimal maxMarks)
    {
        if (marks.Count == 0) return 0m;

        var sorted = marks.OrderByDescending(m => m).ToList();

        decimal raw = rule switch
        {
            "Best1" => sorted.First(),
            "Best2" => sorted.Take(2).Average(),
            "Best3" => sorted.Take(3).Average(),
            "All" => sorted.Average(),
            _ => sorted.Average()
        };

        return Math.Round(Math.Min(raw, maxMarks), 2);
    }
}
