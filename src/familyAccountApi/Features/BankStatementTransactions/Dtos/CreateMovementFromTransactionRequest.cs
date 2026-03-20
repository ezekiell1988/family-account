using System.ComponentModel;
using System.ComponentModel.DataAnnotations;

namespace FamilyAccountApi.Features.BankStatementTransactions.Dtos;

/// <summary>
/// Datos adicionales para confirmar y crear un BankMovement a partir de una transacción clasificada.
/// La transacción debe tener IdBankMovementType asignado antes de llamar a este endpoint.
/// </summary>
public sealed record CreateMovementFromTransactionRequest
{
    [Required, Range(1, int.MaxValue)]
    [Description("ID del período fiscal al que pertenece el movimiento")]
    public required int IdFiscalPeriod { get; init; }

    [StringLength(50)]
    [Description("Número de movimiento (ej. MOV-2026-001). Si se omite se genera automáticamente.")]
    public string? NumberMovement { get; init; }

    [Required]
    [Description("Estado inicial del movimiento: 'Borrador' o 'Confirmado'")]
    public required string StatusMovement { get; init; } = "Borrador";

    [Range(typeof(decimal), "0.0001", "999999999999.9999")]
    [Description("Tipo de cambio vigente. Por defecto 1.")]
    public decimal ExchangeRateValue { get; init; } = 1m;

    [StringLength(500)]
    [Description("Descripción del movimiento. Si se omite se usa la descripción de la transacción bancaria.")]
    public string? DescriptionOverride { get; init; }
}
