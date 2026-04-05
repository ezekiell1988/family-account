using System.ComponentModel;
using System.ComponentModel.DataAnnotations;

namespace FamilyAccountApi.Features.Warehouses.Dtos;

public sealed record UpdateWarehouseRequest
{
    [Required, StringLength(100, MinimumLength = 1)]
    [Description("Nombre descriptivo del almacén. Debe ser único.")]
    public required string NameWarehouse { get; init; }

    [Description("Indica si este es el almacén predeterminado.")]
    public bool IsDefault { get; init; }

    [Description("Almacén activo.")]
    public bool IsActive { get; init; }
}
