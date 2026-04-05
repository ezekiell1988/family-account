using System.ComponentModel;
using System.ComponentModel.DataAnnotations;

namespace FamilyAccountApi.Features.InventoryLots.Dtos;

public sealed record UpdateInventoryLotStatusRequest
{
    [Required]
    [Description("Nuevo estado del lote: Disponible | Cuarentena | Bloqueado | Vencido")]
    public required string StatusLot { get; init; }
}
