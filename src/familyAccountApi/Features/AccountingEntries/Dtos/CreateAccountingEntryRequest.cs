using System.ComponentModel;
using System.ComponentModel.DataAnnotations;

namespace FamilyAccountApi.Features.AccountingEntries.Dtos;

public sealed record CreateAccountingEntryRequest
{
    [Required, Range(1, int.MaxValue)]
    [Description("Período fiscal al que pertenece el asiento")]
    public required int IdFiscalPeriod { get; init; }

    [Required, Range(1, int.MaxValue)]
    [Description("Moneda en la que se registra el asiento contable")]
    public required int IdCurrency { get; init; }

    [Required, StringLength(30, MinimumLength = 1)]
    [Description("Número o consecutivo del asiento contable")]
    public required string NumberEntry { get; init; }

    [Required]
    [Description("Fecha contable del asiento")]
    public required DateOnly DateEntry { get; init; }

    [Required, StringLength(300, MinimumLength = 1)]
    [Description("Descripción general del asiento")]
    public required string DescriptionEntry { get; init; }

    [Required]
    [AllowedValues("Borrador", "Publicado", "Anulado", ErrorMessage = "El estado debe ser 'Borrador', 'Publicado' o 'Anulado'.")]
    [Description("Estado del asiento: Borrador | Publicado | Anulado")]
    public required string StatusEntry { get; init; }

    [StringLength(100)]
    [Description("Referencia opcional del asiento, por ejemplo número de documento")]
    public string? ReferenceEntry { get; init; }

    [Range(typeof(decimal), "0.000001", "999999999999999.999999", ParseLimitsInInvariantCulture = true, ConvertValueInInvariantCulture = true)]
    [Description("Tipo de cambio usado al registrar el asiento")]
    public required decimal ExchangeRateValue { get; init; }

    [Required, MinLength(2)]
    [Description("Líneas contables del asiento. Debe incluir al menos dos movimientos")]
    public required IReadOnlyList<AccountingEntryLineRequest> Lines { get; init; }
}
