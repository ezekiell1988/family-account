using System.ComponentModel;
using System.ComponentModel.DataAnnotations;

namespace FamilyAccountApi.Features.FinancialObligations.Dtos;

public sealed record CreateFinancialObligationRequest
{
    [Required, StringLength(200, MinimumLength = 1)]
    [Description("Nombre descriptivo del préstamo. Ej: Préstamo COOPEALIANZA CRC")]
    public required string NameObligation { get; init; }

    [Required, Range(1, int.MaxValue)]
    [Description("FK a la moneda del préstamo")]
    public required int IdCurrency { get; init; }

    [Required, Range(typeof(decimal), "0.01", "999999999999", ParseLimitsInInvariantCulture = true)]
    [Description("Monto original desembolsado")]
    public required decimal OriginalAmount { get; init; }

    [Required, Range(typeof(decimal), "0", "100", ParseLimitsInInvariantCulture = true, ConvertValueInInvariantCulture = true)]
    [Description("Tasa de interés anual. Ej: 18.50 = 18.5%. Usar 0 para Tasa Cero (BAC Financiamientos).")]
    public required decimal InterestRate { get; init; }

    [Required]
    [Description("Fecha de primer desembolso o primer vencimiento")]
    public required DateOnly StartDate { get; init; }

    [Required, Range(1, 600)]
    [Description("Plazo total en meses")]
    public required int TermMonths { get; init; }

    [Range(1, int.MaxValue)]
    [Description("FK a la cuenta bancaria BAC desde la que se pagan las cuotas")]
    public int? IdBankAccountPayment { get; init; }

    [Required, Range(1, int.MaxValue)]
    [Description("FK a cuenta de Pasivo No Corriente. Ej: 2.2.01.01")]
    public required int IdAccountLongTerm { get; init; }

    [Required, Range(1, int.MaxValue)]
    [Description("FK a cuenta de Pasivo Corriente — porción corriente. Ej: 2.1.02.01")]
    public required int IdAccountShortTerm { get; init; }

    [Required, Range(1, int.MaxValue)]
    [Description("FK a cuenta de Gasto Intereses. Ej: 5.5.05")]
    public required int IdAccountInterest { get; init; }

    [Range(1, int.MaxValue)]
    [Description("FK a cuenta de Gasto Mora. Null usa la misma de intereses")]
    public int? IdAccountLateFee { get; init; }

    [Range(1, int.MaxValue)]
    [Description("FK a cuenta de Gasto Otros cargos")]
    public int? IdAccountOther { get; init; }

    [Required, StringLength(100, MinimumLength = 1)]
    [Description("Keyword para matching automático en movimientos BAC. Ej: COOPEALIANZA")]
    public required string MatchKeyword { get; init; }

    [StringLength(500)]
    [Description("Observaciones adicionales")]
    public string? Notes { get; init; }
}
