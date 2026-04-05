using System.ComponentModel;
using System.ComponentModel.DataAnnotations;

namespace FamilyAccountApi.Features.InventoryAdjustmentTypes.Dtos;

public sealed record UpdateInventoryAdjustmentTypeRequest
{
    [Required, StringLength(150, MinimumLength = 1)]
    [Description("Nombre descriptivo del tipo de ajuste")]
    public required string NameInventoryAdjustmentType { get; init; }

    [Range(1, int.MaxValue)]
    [Description("FK a la cuenta de activo de inventario: DR en entradas, CR en salidas.")]
    public int? IdAccountInventoryDefault { get; init; }

    [Range(1, int.MaxValue)]
    [Description("FK a la cuenta contrapartida para entradas (delta > 0).")]
    public int? IdAccountCounterpartEntry { get; init; }

    [Range(1, int.MaxValue)]
    [Description("FK a la cuenta contrapartida para salidas (delta < 0).")]
    public int? IdAccountCounterpartExit { get; init; }

    [Required]
    [Description("Indica si el tipo está activo")]
    public required bool IsActive { get; init; }
}
