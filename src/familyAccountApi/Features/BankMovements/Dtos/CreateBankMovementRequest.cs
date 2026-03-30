using System.ComponentModel;
using System.ComponentModel.DataAnnotations;

namespace FamilyAccountApi.Features.BankMovements.Dtos;

public sealed record CreateBankMovementRequest
{
    [Required, Range(1, int.MaxValue)]
    [Description("ID de la cuenta bancaria afectada")]
    public required int IdBankAccount { get; init; }

    [Required, Range(1, int.MaxValue)]
    [Description("ID del tipo de movimiento bancario")]
    public required int IdBankMovementType { get; init; }

    [Required, Range(1, int.MaxValue)]
    [Description("ID del período fiscal")]
    public required int IdFiscalPeriod { get; init; }

    [Required, StringLength(50, MinimumLength = 1)]
    [Description("Número único del movimiento (ej. MOV-2025-001)")]
    public required string NumberMovement { get; init; }

    [Required]
    [Description("Fecha del movimiento bancario")]
    public required DateOnly DateMovement { get; init; }

    [Required, StringLength(500, MinimumLength = 1)]
    [Description("Descripción del movimiento bancario")]
    public required string DescriptionMovement { get; init; }

    [Range(typeof(decimal), "0.01", "999999999999.99", ParseLimitsInInvariantCulture = true, ConvertValueInInvariantCulture = true)]
    [Description("Monto del movimiento en la moneda de la cuenta bancaria")]
    public decimal Amount { get; init; }

    [Required]
    [Description("Estado inicial del movimiento: 'Borrador' o 'Confirmado'")]
    public required string StatusMovement { get; init; }

    [StringLength(200)]
    [Description("Referencia externa (número de cheque, comprobante, etc.)")]
    public string? ReferenceMovement { get; init; }

    [Range(typeof(decimal), "0.0001", "999999999999.9999", ParseLimitsInInvariantCulture = true, ConvertValueInInvariantCulture = true)]
    [Description("Tipo de cambio vigente al momento del movimiento")]
    public decimal ExchangeRateValue { get; init; }

    [Description("Documentos de soporte del movimiento")]
    public IReadOnlyList<BankMovementDocumentRequest> Documents { get; init; } = [];
}
