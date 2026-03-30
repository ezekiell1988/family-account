using System.ComponentModel;
using System.ComponentModel.DataAnnotations;

namespace FamilyAccountApi.Features.PurchaseInvoices.Dtos;

public sealed record PurchaseInvoiceLineRequest
{
    [Range(1, int.MaxValue)]
    [Description("ID del SKU del producto escaneado (opcional)")]
    public int? IdProductSKU { get; init; }

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
