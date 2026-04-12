using System.ComponentModel;
using System.ComponentModel.DataAnnotations;

namespace FamilyAccountApi.Features.FinancialObligations.Dtos;

public sealed record UpdateFinancialObligationRequest
{
    [Required, StringLength(200, MinimumLength = 1)]
    [Description("Nombre descriptivo del préstamo")]
    public required string NameObligation { get; init; }

    [Range(1, int.MaxValue)]
    [Description("FK a la cuenta bancaria BAC de pago")]
    public int? IdBankAccountPayment { get; init; }

    [Required, Range(1, int.MaxValue)]
    [Description("FK a cuenta de Pasivo No Corriente")]
    public required int IdAccountLongTerm { get; init; }

    [Required, Range(1, int.MaxValue)]
    [Description("FK a cuenta de Pasivo Corriente — porción corriente")]
    public required int IdAccountShortTerm { get; init; }

    [Required, Range(1, int.MaxValue)]
    [Description("FK a cuenta de Gasto Intereses")]
    public required int IdAccountInterest { get; init; }

    [Range(1, int.MaxValue)]
    [Description("FK a cuenta de Gasto Mora")]
    public int? IdAccountLateFee { get; init; }

    [Range(1, int.MaxValue)]
    [Description("FK a cuenta de Gasto Otros")]
    public int? IdAccountOther { get; init; }

    [Required, StringLength(100, MinimumLength = 1)]
    [Description("Keyword para matching automático en movimientos BAC")]
    public required string MatchKeyword { get; init; }

    [Required]
    [AllowedValues("Activo", "Liquidado", ErrorMessage = "El estado debe ser 'Activo' o 'Liquidado'.")]
    [Description("Estado del préstamo: Activo | Liquidado")]
    public required string StatusObligation { get; init; }

    [StringLength(500)]
    [Description("Observaciones adicionales")]
    public string? Notes { get; init; }
}
