using System.ComponentModel;
using System.ComponentModel.DataAnnotations;

namespace FamilyAccountApi.Features.Products.Dtos;

public sealed record CreateProductRequest
{
    [Required, StringLength(50, MinimumLength = 1)]
    [Description("Código único del producto")]
    public required string CodeProduct { get; init; }

    [Required, StringLength(200, MinimumLength = 1)]
    [Description("Nombre del producto")]
    public required string NameProduct { get; init; }

    [Required]
    [Description("ID del tipo de producto (FK a productType)")]
    public required int IdProductType { get; init; }

    [Required]
    [Description("ID de la unidad base de inventario (FK a unitOfMeasure)")]
    public required int IdUnit { get; init; }

    [Description("ID del producto padre para agrupar variantes (opcional, máximo 1 nivel)")]
    public int? IdProductParent { get; init; }

    [Description("Indica que el producto tiene opciones configurables")]
    public bool HasOptions { get; init; } = false;

    [Description("Indica que el producto es un combo de slots")]
    public bool IsCombo { get; init; } = false;
}
