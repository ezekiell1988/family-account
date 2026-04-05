using System.ComponentModel;
using System.ComponentModel.DataAnnotations;

namespace FamilyAccountApi.Features.ProductionOrders.Dtos;

public sealed record CreateProductionOrderRequest
{
    [Required]
    [Description("FK al período fiscal")]
    public required int IdFiscalPeriod { get; init; }

    [Description("FK al pedido de venta. NULL = Modalidad A (producción para stock).")]
    public int? IdSalesOrder { get; init; }

    [Required]
    [Description("Fecha de la orden")]
    public required DateOnly DateOrder { get; init; }

    [Description("Fecha requerida de producción. Opcional.")]
    public DateOnly? DateRequired { get; init; }

    [StringLength(500)]
    [Description("Observaciones opcionales")]
    public string? DescriptionOrder { get; init; }

    [Required]
    [MinLength(1)]
    [Description("Líneas de producción (productos a producir)")]
    public required IReadOnlyList<ProductionOrderLineRequest> Lines { get; init; }
}

public sealed record ProductionOrderLineRequest
{
    [Required]
    [Description("FK al producto final a producir")]
    public required int IdProduct { get; init; }

    [Required]
    [Description("FK a la unidad de producción")]
    public required int IdProductUnit { get; init; }

    [Description("FK a la línea del pedido de venta de origen. Opcional.")]
    public int? IdSalesOrderLine { get; init; }

    [Required]
    [Range(typeof(decimal), "0.0001", "999999999.9999",
        ParseLimitsInInvariantCulture = true, ConvertValueInInvariantCulture = true)]
    [Description("Cantidad total a producir en unidad base")]
    public required decimal QuantityRequired { get; init; }

    [StringLength(500)]
    [Description("Nota opcional de la línea")]
    public string? DescriptionLine { get; init; }
}
