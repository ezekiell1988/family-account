using System.ComponentModel;
using System.ComponentModel.DataAnnotations;

namespace FamilyAccountApi.Features.BankMovementTypes.Dtos;

public sealed record CreateBankMovementTypeRequest
{
    [Required, StringLength(20, MinimumLength = 1)]
    [Description("Código único del tipo de movimiento (ej. DEP, RET, PAGO)")]
    public required string CodeBankMovementType { get; init; }

    [Required, StringLength(150, MinimumLength = 1)]
    [Description("Nombre descriptivo del tipo de movimiento")]
    public required string NameBankMovementType { get; init; }

    [Required, Range(1, int.MaxValue)]
    [Description("ID de la cuenta contable contrapartida por defecto")]
    public required int IdAccountCounterpart { get; init; }

    [Required]
    [Description("Signo del movimiento: 'Cargo' o 'Abono'")]
    public required string MovementSign { get; init; }

    [Description("Indica si el tipo de movimiento está activo")]
    public bool IsActive { get; init; } = true;
}
