using System.ComponentModel;
using System.ComponentModel.DataAnnotations;

namespace FamilyAccountApi.Features.InventoryAdjustments.Dtos;

// ── Request ──────────────────────────────────────────────────────────────────

public sealed record CycleCountLineRequest
{
    [Required]
    [Range(1, int.MaxValue)]
    [Description("ID del lote de inventario a contar físicamente.")]
    public required int IdInventoryLot { get; init; }

    [Required]
    [Range(typeof(decimal), "0", "999999999999.999999",
        ParseLimitsInInvariantCulture = true, ConvertValueInInvariantCulture = true)]
    [Description("Cantidad física contada en unidad base del producto.")]
    public required decimal QuantityPhysical { get; init; }

    [StringLength(500)]
    [Description("Observación opcional sobre este lote durante el conteo.")]
    public string? DescriptionLine { get; init; }
}

public sealed record CreateCycleCountRequest
{
    [Required]
    [Description("ID del período fiscal.")]
    public required int IdFiscalPeriod { get; init; }

    [Required]
    [Range(1, int.MaxValue)]
    [Description("ID de la moneda del ajuste.")]
    public required int IdCurrency { get; init; }

    [Required]
    [Range(typeof(decimal), "0.000001", "999999999.999999",
        ParseLimitsInInvariantCulture = true, ConvertValueInInvariantCulture = true)]
    [Description("Tipo de cambio vigente. Usar 1.0 para moneda local.")]
    public required decimal ExchangeRateValue { get; init; }

    [Required]
    [Description("Fecha del conteo físico.")]
    public required DateOnly DateAdjustment { get; init; }

    [StringLength(500)]
    [Description("Descripción general del conteo (ej. 'Conteo mensual bodega A').")]
    public string? DescriptionAdjustment { get; init; }

    [Required]
    [MinLength(1, ErrorMessage = "Debe incluir al menos un lote en el conteo.")]
    [Description("Lotes contados físicamente.")]
    public required IReadOnlyList<CycleCountLineRequest> Lines { get; init; }
}

// ── Preview ───────────────────────────────────────────────────────────────────

public sealed record CycleCountPreviewLineResponse(
    int      IdInventoryLot,
    string?  LotNumber,
    string   NameProduct,
    decimal  QuantityBook,
    decimal  QuantityPhysical,
    decimal  QuantityDelta,
    string?  DescriptionLine);

public sealed record CycleCountPreviewResponse(
    int      IdFiscalPeriod,
    int      IdCurrency,
    decimal  ExchangeRateValue,
    DateOnly DateAdjustment,
    string?  DescriptionAdjustment,
    IReadOnlyList<CycleCountPreviewLineResponse> Lines,
    IReadOnlyList<CycleCountPreviewLineResponse> LinesWithDifference);
