using System.ComponentModel;
using System.ComponentModel.DataAnnotations;

namespace FamilyAccountApi.Features.PurchaseInvoices.Dtos;

public sealed record PurchaseInvoiceLineRequest
{
    [Description("ID del producto en catálogo (opcional).")]
    public int? IdProduct { get; init; }

    [Description("ID de la unidad de medida usada en esta línea (opcional).")]
    public int? IdUnit { get; init; }

    [StringLength(100)]
    [Description("Número de lote (opcional).")]
    public string? LotNumber { get; init; }

    [Description("Fecha de vencimiento del lote (opcional).")]
    public DateOnly? ExpirationDate { get; init; }

    [Required, StringLength(500, MinimumLength = 1)]
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
