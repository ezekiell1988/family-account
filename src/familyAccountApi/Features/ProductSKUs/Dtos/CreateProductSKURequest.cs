using System.ComponentModel;
using System.ComponentModel.DataAnnotations;

namespace FamilyAccountApi.Features.ProductSKUs.Dtos;

public sealed record CreateProductSKURequest
{
    [Required, StringLength(48, MinimumLength = 1)]
    [Description("Código único del producto (EAN-13, UPC-A, Code128, etc.)")]
    public required string CodeProductSKU { get; init; }

    [Required, StringLength(200, MinimumLength = 1)]
    [Description("Nombre del producto")]
    public required string NameProductSKU { get; init; }

    [StringLength(100)]
    [Description("Marca del producto (opcional)")]
    public string? BrandProductSKU { get; init; }

    [StringLength(500)]
    [Description("Descripción del producto (opcional)")]
    public string? DescriptionProductSKU { get; init; }

    [StringLength(50)]
    [Description("Contenido neto: '500g', '1L', '6 unidades' (opcional)")]
    public string? NetContent { get; init; }
}
