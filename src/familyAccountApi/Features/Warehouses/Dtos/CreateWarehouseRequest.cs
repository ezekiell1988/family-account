using System.ComponentModel;
using System.ComponentModel.DataAnnotations;

namespace FamilyAccountApi.Features.Warehouses.Dtos;

public sealed record CreateWarehouseRequest
{
    [Required, StringLength(100, MinimumLength = 1)]
    [Description("Nombre descriptivo del almacén. Debe ser único.")]
    public required string NameWarehouse { get; init; }

    [Description("Indica si este es el almacén predeterminado para nuevas entradas de stock cuando no se especifica uno. Al marcar uno como predeterminado los demás pierden esa condición.")]
    public bool IsDefault { get; init; }

    [Description("Almacén activo. Por defecto: true.")]
    public bool IsActive { get; init; } = true;
}
