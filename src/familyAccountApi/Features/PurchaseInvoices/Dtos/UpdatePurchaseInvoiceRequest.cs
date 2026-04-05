using System.ComponentModel;
using System.ComponentModel.DataAnnotations;

namespace FamilyAccountApi.Features.PurchaseInvoices.Dtos;

public sealed record UpdatePurchaseInvoiceRequest
{
    [Required, Range(1, int.MaxValue)]
    [Description("ID del período fiscal")]
    public required int IdFiscalPeriod { get; init; }

    [Required, Range(1, int.MaxValue)]
    [Description("ID de la moneda de la factura")]
    public required int IdCurrency { get; init; }

    [Required, Range(1, int.MaxValue)]
    [Description("ID del tipo de factura de compra (EFECTIVO, DEBITO, TC)")]
    public required int IdPurchaseInvoiceType { get; init; }

    [Range(1, int.MaxValue)]
    [Description("ID de la cuenta bancaria (requerido cuando el tipo requiere contrapartida bancaria)")]
    public int? IdBankAccount { get; init; }

    [Range(1, int.MaxValue)]
    [Description("ID del contacto proveedor. Si se omite se crea uno nuevo con el nombre indicado en ProviderName.")]
    public int? IdContact { get; init; }

    [Required, StringLength(100, MinimumLength = 1)]
    [Description("Número de factura del proveedor")]
    public required string NumberInvoice { get; init; }

    [StringLength(200, MinimumLength = 1)]
    [Description("Nombre del proveedor. Obligatorio si no se envía IdContact; se usa para crear un contacto nuevo de tipo Proveedor.")]
    public string? ProviderName { get; init; }

    [Required]
    [Description("Fecha de emisión de la factura")]
    public required DateOnly DateInvoice { get; init; }

    [Required]
    [Range(typeof(decimal), "0", "999999999999.99", ParseLimitsInInvariantCulture = true, ConvertValueInInvariantCulture = true)]
    [Description("Subtotal antes de impuestos")]
    public required decimal SubTotalAmount { get; init; }

    [Required]
    [Range(typeof(decimal), "0", "999999999999.99", ParseLimitsInInvariantCulture = true, ConvertValueInInvariantCulture = true)]
    [Description("Total de impuestos")]
    public required decimal TaxAmount { get; init; }

    [Required]
    [Range(typeof(decimal), "0.01", "999999999999.99", ParseLimitsInInvariantCulture = true, ConvertValueInInvariantCulture = true)]
    [Description("Total general de la factura")]
    public required decimal TotalAmount { get; init; }

    [StringLength(500)]
    [Description("Notas adicionales opcionales")]
    public string? DescriptionInvoice { get; init; }

    [Required]
    [Range(1, int.MaxValue)]
    [Description("ID del almacén destino de la mercadería. Opcional; si se omite se conserva el valor actual.")]
    public int? IdWarehouse { get; init; }

    [Range(typeof(decimal), "0.0001", "999999999999.9999", ParseLimitsInInvariantCulture = true, ConvertValueInInvariantCulture = true)]
    [Description("Tipo de cambio al momento del registro")]
    public required decimal ExchangeRateValue { get; init; }

    [Description("Líneas de la factura (reemplaza completamente las líneas existentes)")]
    public IReadOnlyList<PurchaseInvoiceLineRequest> Lines { get; init; } = [];
}
