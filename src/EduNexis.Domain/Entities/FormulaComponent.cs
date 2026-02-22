namespace EduNexis.Domain.Entities;

public class FormulaComponent : BaseEntity
{
    public Guid FormulaId { get; private set; }
    public FormulaComponentType ComponentType { get; private set; }
    public string SelectionRule { get; private set; } = string.Empty;
    public decimal WeightPercent { get; private set; }
    public decimal MaxMarks { get; private set; }

    // Navigation
    public GradingFormula Formula { get; private set; } = null!;

    protected FormulaComponent() { }

    public static FormulaComponent Create(
        Guid formulaId, FormulaComponentType type,
        string selectionRule, decimal weightPercent, decimal maxMarks)
    {
        if (weightPercent <= 0 || weightPercent > 100)
            throw new DomainException("Weight percent must be between 1 and 100.");

        return new FormulaComponent
        {
            FormulaId = formulaId,
            ComponentType = type,
            SelectionRule = selectionRule,
            WeightPercent = weightPercent,
            MaxMarks = maxMarks
        };
    }
}
