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
) : ICommand<ApiResponse>;

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

        if (course.TeacherId != command.TeacherId)
            throw new UnauthorizedException("Only the teacher can define the grading formula.");

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
