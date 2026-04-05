using System.ComponentModel;
using System.ComponentModel.DataAnnotations;

namespace FamilyAccountApi.Features.SalesOrders.Dtos;

public sealed record UpdateSalesOrderRequest
{
    [Required]
    [Description("FK al período fiscal")]
    public required int IdFiscalPeriod { get; init; }

    [Required]
    [Description("FK a la moneda del pedido")]
    public required int IdCurrency { get; init; }

    [Required]
    [Description("FK al cliente")]
    public required int IdContact { get; init; }

    [Description("FK a la lista de precios de referencia. Opcional.")]
    public int? IdPriceList { get; init; }

    [Required]
    [Description("Fecha del pedido")]
    public required DateOnly DateOrder { get; init; }

    [Description("Fecha compromiso de entrega. Opcional.")]
    public DateOnly? DateDelivery { get; init; }

    [Required]
    [Range(typeof(decimal), "1", "999999999999.99",
        ParseLimitsInInvariantCulture = true, ConvertValueInInvariantCulture = true)]
    [Description("Tipo de cambio vigente")]
    public required decimal ExchangeRateValue { get; init; }

    [StringLength(500)]
    [Description("Observaciones opcionales")]
    public string? DescriptionOrder { get; init; }

    [Required]
    [MinLength(1)]
    [Description("Líneas del pedido (reemplaza las existentes)")]
    public required IReadOnlyList<SalesOrderLineRequest> Lines { get; init; }
}
