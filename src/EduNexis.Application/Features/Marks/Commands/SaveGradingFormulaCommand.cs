using EduNexis.Application.Abstractions;
namespace EduNexis.Application.Features.Marks.Commands;

public record FormulaComponentRequest(
    FormulaComponentType ComponentType,
    string SelectionRule,
    decimal WeightPercent,
    decimal MaxMarks
);

public record SaveGradingFormulaCommand(
    Guid CourseId,
    Guid TeacherId,
    decimal TotalMarks,
    List<FormulaComponentRequest> Components
) : ICommand<ApiResponse>, ICourseScopedWrite
{
    public ValueTask<Guid?> ResolveCourseIdAsync(IUnitOfWork uow, CancellationToken ct)
        => ValueTask.FromResult<Guid?>(CourseId);
}

public sealed class SaveGradingFormulaCommandValidator : AbstractValidator<SaveGradingFormulaCommand>
{
    public SaveGradingFormulaCommandValidator()
    {
        RuleFor(x => x.TotalMarks).GreaterThan(0);
        RuleFor(x => x.Components).NotEmpty();
        RuleForEach(x => x.Components).ChildRules(c =>
        {
            c.RuleFor(x => x.WeightPercent).GreaterThan(0).LessThanOrEqualTo(100);
            c.RuleFor(x => x.MaxMarks).GreaterThan(0);
            c.RuleFor(x => x.SelectionRule).NotEmpty();
        });
    }
}

public sealed class SaveGradingFormulaCommandHandler(
    IUnitOfWork uow
) : ICommandHandler<SaveGradingFormulaCommand, ApiResponse>
{
    public async ValueTask<ApiResponse> Handle(
        SaveGradingFormulaCommand command, CancellationToken ct)
    {
        var course = await uow.Courses.GetByIdAsync(command.CourseId, ct)
            ?? throw new NotFoundException("Course", command.CourseId);

        if (!await CourseAccess.IsTeacherAsync(uow, course, command.TeacherId, ct))
            throw new UnauthorizedException("Only the teacher can define the grading formula.");

        var publishedMarks = await uow.GetRepository<FinalMark>()
            .FindAsync(fm => fm.CourseId == command.CourseId && fm.IsPublished, ct);

        if (publishedMarks.Any())
            return ApiResponse.Fail("Cannot change grading formula while final marks are published. Please unpublish final marks first.");

        // Validate component prerequisites based on actual items in course
        foreach (var comp in command.Components)
        {
            switch (comp.ComponentType)
            {
                case FormulaComponentType.CT:
                    var ctEvents = await uow.GetRepository<CTEvent>()
                        .FindAsync(e => e.CourseId == command.CourseId, ct);
                    int ctCount = ctEvents.Count();
                    if (ctCount == 0)
                        return ApiResponse.Fail("Cannot include Class Tests in grading formula: no class tests exist in this course.");
                    if (comp.SelectionRule == "Best3" && ctCount < 3)
                        return ApiResponse.Fail($"Cannot select 'Best 3' for Class Tests: only {ctCount} CT(s) exist in this course.");
                    if (comp.SelectionRule == "Best2" && ctCount < 2)
                        return ApiResponse.Fail($"Cannot select 'Best 2' for Class Tests: only {ctCount} CT(s) exist in this course.");
                    break;

                case FormulaComponentType.Assignment:
                    var assignments = await uow.GetRepository<Assignment>()
                        .FindAsync(a => a.CourseId == command.CourseId, ct);
                    int assignmentCount = assignments.Count();
                    if (assignmentCount == 0)
                        return ApiResponse.Fail("Cannot include Assignments in grading formula: no assignments exist in this course.");
                    if (comp.SelectionRule == "Best3" && assignmentCount < 3)
                        return ApiResponse.Fail($"Cannot select 'Best 3' for Assignments: only {assignmentCount} assignment(s) exist.");
                    if (comp.SelectionRule == "Best2" && assignmentCount < 2)
                        return ApiResponse.Fail($"Cannot select 'Best 2' for Assignments: only {assignmentCount} assignment(s) exist.");
                    break;

                case FormulaComponentType.Presentation:
                    var presentations = await uow.GetRepository<PresentationEvent>()
                        .FindAsync(p => p.CourseId == command.CourseId, ct);
                    int presCount = presentations.Count();
                    if (presCount == 0)
                        return ApiResponse.Fail("Cannot include Other Tests in grading formula: no presentations exist in this course.");
                    if (comp.SelectionRule == "Best3" && presCount < 3)
                        return ApiResponse.Fail($"Cannot select 'Best 3' for Other Tests: only {presCount} presentation(s) exist.");
                    if (comp.SelectionRule == "Best2" && presCount < 2)
                        return ApiResponse.Fail($"Cannot select 'Best 2' for Other Tests: only {presCount} presentation(s) exist.");
                    break;

                case FormulaComponentType.Attendance:
                    var sessions = await uow.GetRepository<AttendanceSession>()
                        .FindAsync(s => s.CourseId == command.CourseId, ct);
                    int sessionCount = sessions.Count();
                    if (sessionCount == 0)
                        return ApiResponse.Fail("Cannot include Attendance in grading formula: no attendance sessions recorded yet.");
                    break;
            }
        }

        // Get or create formula
        var formula = await uow.GetRepository<GradingFormula>()
            .FirstOrDefaultAsync(f => f.CourseId == command.CourseId, ct);

        bool isNew = formula is null;

        if (isNew)
        {
            formula = GradingFormula.Create(command.CourseId, command.TotalMarks);
            await uow.GetRepository<GradingFormula>().AddAsync(formula, ct);
            await uow.SaveChangesAsync(ct); // save to get formula.Id
        }
        else
        {
            formula!.UpdateTotalMarks(command.TotalMarks);

            // Delete old components
            var oldComponents = await uow.GetRepository<FormulaComponent>()
                .FindAsync(c => c.FormulaId == formula.Id, ct);

            foreach (var old in oldComponents)
                uow.GetRepository<FormulaComponent>().Delete(old);
        }

        // Add new components
        foreach (var comp in command.Components)
        {
            var component = FormulaComponent.Create(
                formula!.Id,
                comp.ComponentType,
                comp.SelectionRule,
                comp.WeightPercent,
                comp.MaxMarks);

            await uow.GetRepository<FormulaComponent>().AddAsync(component, ct);
        }

        await uow.SaveChangesAsync(ct);
        return ApiResponse.Ok("Grading formula saved successfully.");
    }
}
