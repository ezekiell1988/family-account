using System.ComponentModel;
using System.ComponentModel.DataAnnotations;

namespace FamilyAccountApi.Features.ExchangeRates.Dtos;

public sealed record UpdateExchangeRateRequest
{
    [Required]
    [Description("Moneda a la que pertenece el tipo de cambio")]
    public required int IdCurrency { get; init; }

    [Required]
    [Description("Fecha efectiva del tipo de cambio")]
    public required DateOnly RateDate { get; init; }

    [Range(typeof(decimal), "0.000001", "999999999999999.999999")]
    [Description("Valor del tipo de cambio")]
    public required decimal RateValue { get; init; }
}