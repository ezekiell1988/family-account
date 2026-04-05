using System.ComponentModel;
using System.ComponentModel.DataAnnotations;

namespace FamilyAccountApi.Features.ProductionOrders.Dtos;

public sealed record UpdateProductionOrderRequest
{
    [Required]
    [Description("FK al período fiscal")]
    public required int IdFiscalPeriod { get; init; }

    [Description("FK al pedido de venta. NULL = Modalidad A.")]
    public int? IdSalesOrder { get; init; }

    [Description("Bodega de producción (consumo de MP y entrada del PT).")]
    public int? IdWarehouse { get; init; }

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
    [Description("Líneas de producción (reemplaza las existentes)")]
    public required IReadOnlyList<ProductionOrderLineRequest> Lines { get; init; }
}

public sealed record UpdateProductionOrderStatusRequest
{
    [Required]
    [RegularExpression("^(Pendiente|EnProceso|Completado|Anulado)$",
        ErrorMessage = "Estado inválido. Valores válidos: Pendiente, EnProceso, Completado, Anulado")]
    [Description("Nuevo estado de la orden")]
    public required string StatusProductionOrder { get; init; }

    [Description("Override de bodega al completar. Si no se envía, se usa la bodega registrada en la orden.")]
    public int? IdWarehouse { get; init; }

    [Description("Cantidades reales producidas por línea al completar. Si se omite una línea se usa QuantityRequired.")]
    public IReadOnlyList<CompleteProductionOrderLineRequest>? Lines { get; init; }
}

public sealed record CompleteProductionOrderLineRequest
{
    [Required]
    [Description("FK a la línea de la orden de producción")]
    public required int IdProductionOrderLine { get; init; }

    [Required]
    [Range(typeof(decimal), "0.0001", "999999999.9999",
        ParseLimitsInInvariantCulture = true, ConvertValueInInvariantCulture = true)]
    [Description("Cantidad realmente producida en unidad base")]
    public required decimal QuantityProduced { get; init; }
}
