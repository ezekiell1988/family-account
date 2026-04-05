using System.ComponentModel;
using System.ComponentModel.DataAnnotations;

namespace FamilyAccountApi.Features.InventoryAdjustmentTypes.Dtos;

public sealed record CreateInventoryAdjustmentTypeRequest
{
    [Required, StringLength(20, MinimumLength = 1)]
    [Description("Código único del tipo (ej. 'CONTEO', 'PRODUCCION', 'AJUSTE_COSTO')")]
    public required string CodeInventoryAdjustmentType { get; init; }

    [Required, StringLength(150, MinimumLength = 1)]
    [Description("Nombre descriptivo del tipo de ajuste")]
    public required string NameInventoryAdjustmentType { get; init; }

    [Range(1, int.MaxValue)]
    [Description("FK a la cuenta de activo de inventario: DR en entradas, CR en salidas. Si null, no se genera asiento al confirmar.")]
    public int? IdAccountInventoryDefault { get; init; }

    [Range(1, int.MaxValue)]
    [Description("FK a la cuenta contrapartida para entradas (delta > 0): actúa como CR del asiento (ej. 'Ajuste Favorable de Inventario').")]
    public int? IdAccountCounterpartEntry { get; init; }

    [Range(1, int.MaxValue)]
    [Description("FK a la cuenta contrapartida para salidas (delta < 0): actúa como DR del asiento (ej. 'Gasto por Merma').")]
    public int? IdAccountCounterpartExit { get; init; }

    [Required]
    [Description("Indica si el tipo está activo")]
    public required bool IsActive { get; init; }
}
