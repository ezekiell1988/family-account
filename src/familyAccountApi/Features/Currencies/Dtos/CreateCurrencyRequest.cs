using System.ComponentModel;
using System.ComponentModel.DataAnnotations;

namespace FamilyAccountApi.Features.Currencies.Dtos;

public sealed record CreateCurrencyRequest
{
    [Required, StringLength(10, MinimumLength = 1)]
    [Description("Código único de la moneda")]
    public required string CodeCurrency { get; init; }

    [Required, StringLength(100, MinimumLength = 1)]
    [Description("Nombre descriptivo de la moneda")]
    public required string NameCurrency { get; init; }

    [Required, StringLength(10, MinimumLength = 1)]
    [Description("Símbolo de la moneda")]
    public required string SymbolCurrency { get; init; }
}