using System.ComponentModel;
using System.ComponentModel.DataAnnotations;

namespace FamilyAccountApi.Features.ProductUnits.Dtos;

public sealed record UpdateProductUnitRequest
{
    [Required]
    [Range(typeof(decimal), "0.000001", "999999999999.999999",
        ParseLimitsInInvariantCulture = true, ConvertValueInInvariantCulture = true)]
    [Description("Cuántas unidades base equivale 1 de esta presentación")]
    public required decimal ConversionFactor { get; init; }

    [Required]
    [Description("Indica si es la presentación base del producto")]
    public required bool IsBase { get; init; }

    [Description("Indica si se puede usar en facturas de compra")]
    public bool UsedForPurchase { get; init; } = true;

    [Description("Indica si se puede usar en facturas de venta")]
    public bool UsedForSale { get; init; } = true;

    [StringLength(48)]
    [Description("Código de barras EAN-8, EAN-13 o UPC-A (opcional, único en el sistema)")]
    public string? CodeBarcode { get; init; }

    [StringLength(200)]
    [Description("Nombre comercial del empaque")]
    public string? NamePresentation { get; init; }

    [StringLength(100)]
    [Description("Marca del fabricante del empaque")]
    public string? BrandPresentation { get; init; }

    [Range(typeof(decimal), "0", "999999999999.9999",
        ParseLimitsInInvariantCulture = true, ConvertValueInInvariantCulture = true)]
    [Description("Precio base de venta para esta presentación")]
    public decimal SalePrice { get; init; } = 0m;
}
