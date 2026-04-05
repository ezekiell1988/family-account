using System.ComponentModel;
using System.ComponentModel.DataAnnotations;

namespace FamilyAccountApi.Features.PriceLists.Dtos;

public sealed record CreatePriceListRequest
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

    [Description("Fecha de fin de vigencia. Omitir para vigencia indefinida")]
    public DateOnly? DateTo { get; init; }

    [Required]
    [Description("Ítems de precios de la lista")]
    public required IReadOnlyList<CreatePriceListItemRequest> Items { get; init; }
}

public sealed record CreatePriceListItemRequest
{
    [Required]
    [Description("FK al producto")]
    public required int IdProduct { get; init; }

    [Required]
    [Description("FK a la presentación (unidad de venta)")]
    public required int IdProductUnit { get; init; }

    [Required]
    [Range(typeof(decimal), "0.01", "999999999.99",
        ParseLimitsInInvariantCulture = true, ConvertValueInInvariantCulture = true)]
    [Description("Precio unitario")]
    public required decimal UnitPrice { get; init; }
}
