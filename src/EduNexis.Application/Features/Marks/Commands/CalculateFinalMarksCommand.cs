namespace EduNexis.Application.Features.Marks.Commands;

public record CalculateFinalMarksCommand(
    Guid CourseId,
    Guid TeacherId
) : ICommand<ApiResponse>;

public sealed class CalculateFinalMarksCommandHandler(
    IUnitOfWork uow
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

        var components = await uow.GetRepository<FormulaComponent>()
            .FindAsync(c => c.FormulaId == formula.Id, ct);

        var students = await uow.GetRepository<CourseMember>()
            .FindAsync(m => m.CourseId == command.CourseId, ct);

        foreach (var student in students)
        {
            decimal finalMark = 0;
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

                    _ => 0
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

            // Upsert FinalMark
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
        }

        await uow.SaveChangesAsync(ct);
        return ApiResponse.Ok("Final marks calculated successfully.");
    }

    private async Task<decimal> CalculateCT(
        Guid courseId, Guid studentId, string rule, decimal maxMarks, CancellationToken ct)
    {
        var ctEvents = await uow.GetRepository<CTEvent>()
            .FindAsync(e => e.CourseId == courseId, ct);

        var marks = new List<decimal>();
        foreach (var ev in ctEvents)
        {
            var sub = await uow.GetRepository<CTSubmission>()
                .FirstOrDefaultAsync(s =>
                    s.CTEventId == ev.Id && s.StudentId == studentId, ct);
            if (sub?.Marks is not null)
                marks.Add(sub.Marks.Value);
        }

        return ApplyRule(marks, rule, maxMarks);
    }

    private async Task<decimal> CalculateAssignment(
        Guid courseId, Guid studentId, string rule, decimal maxMarks, CancellationToken ct)
    {
        var assignments = await uow.GetRepository<Assignment>()
            .FindAsync(a => a.CourseId == courseId, ct);

        var marks = new List<decimal>();
        foreach (var a in assignments)
        {
            var sub = await uow.GetRepository<AssignmentSubmission>()
                .FirstOrDefaultAsync(s =>
                    s.AssignmentId == a.Id && s.StudentId == studentId, ct);
            if (sub?.Marks is not null)
                marks.Add(sub.Marks.Value);
        }

        return ApplyRule(marks, rule, maxMarks);
    }

    private async Task<decimal> CalculatePresentation(
        Guid courseId, Guid studentId, string rule, decimal maxMarks, CancellationToken ct)
    {
        var events = await uow.GetRepository<PresentationEvent>()
            .FindAsync(e => e.CourseId == courseId, ct);

        var marks = new List<decimal>();
        foreach (var ev in events)
        {
            var mark = await uow.GetRepository<PresentationMark>()
                .FirstOrDefaultAsync(m =>
                    m.PresentationEventId == ev.Id && m.StudentId == studentId, ct);
            if (mark is not null)
                marks.Add(mark.Marks);
        }

        return ApplyRule(marks, rule, maxMarks);
    }

    private async Task<decimal> CalculateAttendance(
        Guid courseId, Guid studentId, decimal maxMarks, CancellationToken ct)
    {
        var sessions = await uow.GetRepository<AttendanceSession>()
            .FindAsync(s => s.CourseId == courseId, ct);

        var sessionList = sessions.ToList();
        if (sessionList.Count == 0) return 0;

        int present = 0;
        foreach (var session in sessionList)
        {
            var record = await uow.GetRepository<AttendanceRecord>()
                .FirstOrDefaultAsync(r =>
                    r.SessionId == session.Id && r.StudentId == studentId, ct);
            if (record?.Status == AttendanceStatus.Present)
                present++;
        }

        decimal percentage = (decimal)present / sessionList.Count * 100;
        return Math.Round(percentage / 100 * maxMarks, 2);
    }

    private static decimal ApplyRule(List<decimal> marks, string rule, decimal maxMarks)
    {
        if (marks.Count == 0) return 0;

        var sorted = marks.OrderByDescending(m => m).ToList();

        decimal raw = rule switch
        {
            "Best1" => sorted.First(),
            "Best2" => sorted.Take(2).Average(),
            "Best3" => sorted.Take(3).Average(),
            "All" => sorted.Average(),
            _ => sorted.Average()
        };

        // Scale to maxMarks relative to the average of available event maxMarks
        return Math.Round(Math.Min(raw, maxMarks), 2);
    }
}
