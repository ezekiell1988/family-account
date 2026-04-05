using System.ComponentModel;
using System.ComponentModel.DataAnnotations;

namespace FamilyAccountApi.Features.SalesOrders.Dtos;

public sealed record SalesOrderFulfillmentResponse(
    int      IdSalesOrderLineFulfillment,
    int      IdSalesOrderLine,
    string   FulfillmentType,
    int?     IdInventoryLot,
    int?     IdProductionOrder,
    string?  ProductionOrderNumber,
    decimal  QuantityBase,
    decimal? UnitCost,
    DateTime CreatedAt);

public sealed record AddFulfillmentRequest
{
    [Required]
    [Description("FK a la línea del pedido")]
    public required int IdSalesOrderLine { get; init; }

    [Required]
    [RegularExpression("^(Stock|Produccion)$", ErrorMessage = "Debe ser 'Stock' o 'Produccion'")]
    [Description("Tipo de fulfillment: 'Stock' o 'Produccion'")]
    public required string FulfillmentType { get; init; }

    [Description("FK al lote (requerido si FulfillmentType = 'Stock')")]
    public int? IdInventoryLot { get; init; }

    [Description("FK a la orden de producción (requerido si FulfillmentType = 'Produccion')")]
    public int? IdProductionOrder { get; init; }

    [Required]
    [Range(typeof(decimal), "0.0001", "999999999.9999",
        ParseLimitsInInvariantCulture = true, ConvertValueInInvariantCulture = true)]
    [Description("Cantidad en unidad base asignada")]
    public required decimal QuantityBase { get; init; }
}

public sealed record SalesOrderAdvanceResponse(
    int      IdSalesOrderAdvance,
    int      IdSalesOrder,
    int      IdAccountingEntry,
    int?     IdProductionOrder,
    string?  ProductionOrderNumber,
    decimal  Amount,
    DateOnly DateAdvance,
    string?  DescriptionAdvance,
    DateTime CreatedAt);

public sealed record CreateSalesOrderAdvanceRequest
{
    [Required]
    [Description("FK al asiento contable del anticipo")]
    public required int IdAccountingEntry { get; init; }

    [Description("FK informativa a la orden de producción en cuyo contexto se recibió. Opcional.")]
    public int? IdProductionOrder { get; init; }

    [Required]
    [Range(typeof(decimal), "0.01", "999999999.99",
        ParseLimitsInInvariantCulture = true, ConvertValueInInvariantCulture = true)]
    [Description("Monto del anticipo")]
    public required decimal Amount { get; init; }

    [Required]
    [Description("Fecha de recepción del anticipo")]
    public required DateOnly DateAdvance { get; init; }

    [StringLength(500)]
    [Description("Nota opcional")]
    public string? DescriptionAdvance { get; init; }
}
