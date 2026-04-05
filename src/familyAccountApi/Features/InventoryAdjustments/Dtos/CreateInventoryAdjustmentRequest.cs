using System.ComponentModel;
using System.ComponentModel.DataAnnotations;

namespace FamilyAccountApi.Features.InventoryAdjustments.Dtos;

public sealed record InventoryAdjustmentLineRequest
{
    [Required]
    [Description("ID del lote de inventario a ajustar")]
    public required int IdInventoryLot { get; init; }

    [Required]
    [Description("Delta en unidad base: positivo = entrada, negativo = salida, cero = ajuste de costo puro")]
    public required decimal QuantityDelta { get; init; }

    [Range(typeof(decimal), "0.000001", "999999999999.999999",
        ParseLimitsInInvariantCulture = true, ConvertValueInInvariantCulture = true)]
    [Description("Nuevo costo unitario del lote. Requerido si quantityDelta > 0. Reemplaza inventoryLot.unitCost.")]
    public decimal? UnitCostNew { get; init; }

    [StringLength(500)]
    [Description("Detalle de la línea: insumo consumido, merma, motivo del ajuste")]
    public string? DescriptionLine { get; init; }
}

public sealed record CreateInventoryAdjustmentRequest
{
    [Required]
    [Description("ID del período fiscal")]
    public required int IdFiscalPeriod { get; init; }

    [Required]
    [Range(1, int.MaxValue)]
    [Description("ID del tipo de ajuste (ver /inventory-adjustment-types)")]
    public required int IdInventoryAdjustmentType { get; init; }

    [Required]
    [Range(1, int.MaxValue)]
    [Description("ID de la moneda del ajuste")]
    public required int IdCurrency { get; init; }

    [Required]
    [Range(typeof(decimal), "0.000001", "999999999.999999",
        ParseLimitsInInvariantCulture = true, ConvertValueInInvariantCulture = true)]
    [Description("Tipo de cambio vigente. Usar 1.0 para moneda local.")]
    public required decimal ExchangeRateValue { get; init; }

    [Required]
    [Description("Fecha del evento")]
    public required DateOnly DateAdjustment { get; init; }

    [StringLength(500)]
    [Description("Motivo o descripción del ajuste")]
    public string? DescriptionAdjustment { get; init; }

    [Required]
    [Description("Líneas del ajuste")]
    public required IReadOnlyList<InventoryAdjustmentLineRequest> Lines { get; init; }
}
