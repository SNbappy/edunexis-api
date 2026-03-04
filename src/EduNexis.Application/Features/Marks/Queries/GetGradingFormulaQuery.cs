namespace EduNexis.Application.Features.Marks.Queries;

public record GradingFormulaComponentDto(
    string ComponentType,
    string SelectionRule,
    decimal WeightPercent,
    decimal MaxMarks
);

public record GradingFormulaDto(
    Guid Id,
    decimal TotalMarks,
    List<GradingFormulaComponentDto> Components
);

public record GetGradingFormulaQuery(
    Guid CourseId,
    Guid RequesterId
) : IQuery<ApiResponse<GradingFormulaDto?>>;

public sealed class GetGradingFormulaQueryHandler(
    IUnitOfWork uow
) : IQueryHandler<GetGradingFormulaQuery, ApiResponse<GradingFormulaDto?>>
{
    public async ValueTask<ApiResponse<GradingFormulaDto?>> Handle(
        GetGradingFormulaQuery query, CancellationToken ct)
    {
        var formula = await uow.GetRepository<GradingFormula>()
            .FirstOrDefaultAsync(f => f.CourseId == query.CourseId, ct);

        if (formula is null)
            return ApiResponse<GradingFormulaDto?>.Ok(null);

        var components = await uow.GetRepository<FormulaComponent>()
            .FindAsync(c => c.FormulaId == formula.Id, ct);

        var dto = new GradingFormulaDto(
            formula.Id,
            formula.TotalMarks,
            components.Select(c => new GradingFormulaComponentDto(
                c.ComponentType.ToString(),
                c.SelectionRule,
                c.WeightPercent,
                c.MaxMarks
            )).ToList()
        );

        return ApiResponse<GradingFormulaDto?>.Ok(dto);
    }
}
