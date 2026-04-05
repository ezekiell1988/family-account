using System.ComponentModel;
using System.ComponentModel.DataAnnotations;

namespace FamilyAccountApi.Features.ProductAttributes.Dtos;

public sealed record CreateProductAttributeRequest
{
    [Required, StringLength(100, MinimumLength = 1)]
    [Description("Nombre del atributo (ej: Talla, Color, Material)")]
    public required string NameAttribute { get; init; }

    [Range(0, int.MaxValue)]
    [Description("Orden de presentación del atributo dentro del producto padre")]
    public int SortOrder { get; init; } = 0;
}

public sealed record UpdateProductAttributeRequest
{
    [Required, StringLength(100, MinimumLength = 1)]
    [Description("Nombre del atributo (ej: Talla, Color, Material)")]
    public required string NameAttribute { get; init; }

    [Range(0, int.MaxValue)]
    [Description("Orden de presentación del atributo dentro del producto padre")]
    public required int SortOrder { get; init; }
}

public sealed record CreateAttributeValueRequest
{
    [Required, StringLength(100, MinimumLength = 1)]
    [Description("Nombre del valor (ej: S, M, L, Azul, Rojo)")]
    public required string NameValue { get; init; }

    [Range(0, int.MaxValue)]
    [Description("Orden de presentación del valor dentro del atributo")]
    public int SortOrder { get; init; } = 0;
}

public sealed record UpdateAttributeValueRequest
{
    [Required, StringLength(100, MinimumLength = 1)]
    [Description("Nombre del valor (ej: S, M, L, Azul, Rojo)")]
    public required string NameValue { get; init; }

    [Range(0, int.MaxValue)]
    [Description("Orden de presentación del valor dentro del atributo")]
    public required int SortOrder { get; init; }
}
