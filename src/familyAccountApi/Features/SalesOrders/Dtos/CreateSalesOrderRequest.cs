using System.ComponentModel;
using System.ComponentModel.DataAnnotations;

namespace FamilyAccountApi.Features.SalesOrders.Dtos;

public sealed record CreateSalesOrderRequest
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
    [Description("Líneas del pedido")]
    public required IReadOnlyList<SalesOrderLineRequest> Lines { get; init; }
}

public sealed record SalesOrderLineRequest
{
    [Required]
    [Description("FK al producto")]
    public required int IdProduct { get; init; }

    [Required]
    [Description("FK a la presentación (unidad de venta)")]
    public required int IdProductUnit { get; init; }

    [Description("FK al ítem de lista de precios usado para el precio. Opcional.")]
    public int? IdPriceListItem { get; init; }

    [Required]
    [Range(typeof(decimal), "0.0001", "999999999.9999",
        ParseLimitsInInvariantCulture = true, ConvertValueInInvariantCulture = true)]
    [Description("Cantidad en la unidad de venta")]
    public required decimal Quantity { get; init; }

    [Required]
    [Range(typeof(decimal), "0.01", "999999999.99",
        ParseLimitsInInvariantCulture = true, ConvertValueInInvariantCulture = true)]
    [Description("Precio unitario (snapshot de la lista)")]
    public required decimal UnitPrice { get; init; }

    [Required]
    [Range(typeof(decimal), "0", "100",
        ParseLimitsInInvariantCulture = true, ConvertValueInInvariantCulture = true)]
    [Description("Porcentaje de impuesto (ej: 13.00)")]
    public required decimal TaxPercent { get; init; }

    [StringLength(500)]
    [Description("Descripción opcional de la línea")]
    public string? DescriptionLine { get; init; }

    [Description("Opciones configurables seleccionadas para este producto. Opcional.")]
    public IReadOnlyList<SalesOrderLineOptionRequest>? Options { get; init; }
}

public sealed record SalesOrderLineOptionRequest(
    [property: Required] int IdProductOptionItem,
    decimal Quantity = 1m);
