using System.ComponentModel;
using System.ComponentModel.DataAnnotations;

namespace FamilyAccountApi.Features.Budgets.Dtos;

public sealed record UpdateBudgetRequest
{
    [Required, Range(1, int.MaxValue)]
    [Description("Cuenta contable a la que pertenece el presupuesto")]
    public required int IdAccount { get; init; }

    [Required, Range(1, int.MaxValue)]
    [Description("Período fiscal al que aplica el presupuesto")]
    public required int IdFiscalPeriod { get; init; }

    [Range(typeof(decimal), "0.01", "999999999999999.99")]
    [Description("Monto presupuestado")]
    public required decimal AmountBudget { get; init; }

    [StringLength(300)]
    [Description("Notas opcionales del presupuesto")]
    public string? NotesBudget { get; init; }

    [Description("Indica si el presupuesto está activo")]
    public required bool IsActive { get; init; }
}