using System.ComponentModel;
using System.ComponentModel.DataAnnotations;

namespace FamilyAccountApi.Features.PurchaseInvoices.Dtos;

public sealed record PurchaseInvoiceLineRequest
{
    [StringLength(100)]
    [Description("Código del SKU (código de barras, código interno, etc.). Si existe se actualiza el nombre; si no existe se crea.")]
    public string? SkuCode { get; init; }

    [StringLength(300)]
    [Description("Nombre del SKU para crear/actualizar en catálogo. Si no se provee se usa DescriptionLine.")]
    public string? SkuName { get; init; }

    [Required, StringLength(300, MinimumLength = 1)]
    [Description("Descripción de la línea tal como aparece en la factura del proveedor")]
    public required string DescriptionLine { get; init; }

    [Required]
    [Range(typeof(decimal), "0.0001", "999999999999.9999", ParseLimitsInInvariantCulture = true, ConvertValueInInvariantCulture = true)]
    [Description("Cantidad comprada")]
    public required decimal Quantity { get; init; }

    [Required]
    [Range(typeof(decimal), "0.01", "999999999999.99", ParseLimitsInInvariantCulture = true, ConvertValueInInvariantCulture = true)]
    [Description("Precio unitario")]
    public required decimal UnitPrice { get; init; }

    [Required]
    [Range(typeof(decimal), "0", "100", ParseLimitsInInvariantCulture = true, ConvertValueInInvariantCulture = true)]
    [Description("Porcentaje de impuesto (ej. 13.00 para IVA 13%)")]
    public required decimal TaxPercent { get; init; }

    [Required]
    [Range(typeof(decimal), "0.01", "999999999999.99", ParseLimitsInInvariantCulture = true, ConvertValueInInvariantCulture = true)]
    [Description("Total de la línea: Quantity * UnitPrice * (1 + TaxPercent / 100)")]
    public required decimal TotalLineAmount { get; init; }
}
