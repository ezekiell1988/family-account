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
}
