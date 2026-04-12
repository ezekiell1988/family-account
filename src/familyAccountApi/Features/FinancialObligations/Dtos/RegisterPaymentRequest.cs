using System.ComponentModel;
using System.ComponentModel.DataAnnotations;

namespace FamilyAccountApi.Features.FinancialObligations.Dtos;

/// <summary>Registro de pago manual (sin Excel).</summary>
public sealed record RegisterPaymentRequest
{
    [Range(1, int.MaxValue)]
    [Description("FK al movimiento bancario BAC. Null si no aplica")]
    public int? IdBankMovement { get; init; }

    [Required]
    [Description("Fecha real del pago")]
    public required DateOnly DatePayment { get; init; }

    [Required, Range(typeof(decimal), "0.01", "999999999999", ParseLimitsInInvariantCulture = true)]
    [Description("Monto total pagado")]
    public required decimal AmountPaid { get; init; }

    [Required, Range(typeof(decimal), "0", "999999999999", ParseLimitsInInvariantCulture = true)]
    [Description("Capital pagado")]
    public required decimal AmountCapitalPaid { get; init; }

    [Required, Range(typeof(decimal), "0", "999999999999", ParseLimitsInInvariantCulture = true)]
    [Description("Interés pagado")]
    public required decimal AmountInterestPaid { get; init; }

    [Range(typeof(decimal), "0", "999999999999", ParseLimitsInInvariantCulture = true)]
    [Description("Mora pagada")]
    public decimal AmountLatePaid { get; init; } = 0;

    [Range(typeof(decimal), "0", "999999999999", ParseLimitsInInvariantCulture = true)]
    [Description("Otros cargos pagados")]
    public decimal AmountOtherPaid { get; init; } = 0;

    [StringLength(500)]
    [Description("Observaciones del pago")]
    public string? Notes { get; init; }
}
