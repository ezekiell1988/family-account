using System.ComponentModel;
using System.ComponentModel.DataAnnotations;

namespace FamilyAccountApi.Features.PriceLists.Dtos;

public sealed record UpdatePriceListRequest
{
    [Required, StringLength(200, MinimumLength = 1)]
    [Description("Nombre descriptivo de la lista de precios")]
    public required string NamePriceList { get; init; }

    [StringLength(500)]
    [Description("Descripción opcional")]
    public string? Description { get; init; }

    [Required]
    [Description("Fecha de inicio de vigencia")]
    public required DateOnly DateFrom { get; init; }

    [Description("Fecha de fin de vigencia")]
    public DateOnly? DateTo { get; init; }

    [Required]
    [Description("Ítems de precios (reemplaza los existentes)")]
    public required IReadOnlyList<CreatePriceListItemRequest> Items { get; init; }
}
