using System.ComponentModel;
using System.ComponentModel.DataAnnotations;

namespace FamilyAccountApi.Features.Products.Dtos;

public sealed record UpdateProductRequest
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

    [Description("Indica que el producto es un padre que agrupa variantes por atributos. Normalmente lo establece automáticamente el endpoint generate.")]
    public bool IsVariantParent { get; init; } = false;

    [Range(0, double.MaxValue)]
    [Description("Punto de reorden: stock mínimo para disparar alerta de reabastecimiento (opcional)")]
    public decimal? ReorderPoint { get; init; }

    [Range(0, double.MaxValue)]
    [Description("Stock de seguridad reservado que no debe consumirse en operación normal (opcional)")]
    public decimal? SafetyStock { get; init; }

    [Range(0, double.MaxValue)]
    [Description("Cantidad sugerida a pedir al reabastecer (opcional)")]
    public decimal? ReorderQuantity { get; init; }
}
