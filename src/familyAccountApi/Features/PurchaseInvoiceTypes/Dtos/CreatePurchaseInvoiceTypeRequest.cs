using System.ComponentModel;
using System.ComponentModel.DataAnnotations;

namespace FamilyAccountApi.Features.PurchaseInvoiceTypes.Dtos;

public sealed record CreatePurchaseInvoiceTypeRequest
{
    [Required, StringLength(20, MinimumLength = 1)]
    [Description("Código único del tipo (ej. 'EFECTIVO', 'DEBITO', 'TC')")]
    public required string CodePurchaseInvoiceType { get; init; }

    [Required, StringLength(150, MinimumLength = 1)]
    [Description("Nombre descriptivo del tipo de factura")]
    public required string NamePurchaseInvoiceType { get; init; }

    [Required]
    [Description("true = la cuenta CR se toma del BankAccount vinculado; false = cuenta Caja fija")]
    public required bool CounterpartFromBankMovement { get; init; }

    [Range(1, int.MaxValue)]
    [Description("FK a la cuenta Caja CRC (solo para tipo EFECTIVO)")]
    public int? IdAccountCounterpartCRC { get; init; }

    [Range(1, int.MaxValue)]
    [Description("FK a la cuenta Caja USD (solo para tipo EFECTIVO)")]
    public int? IdAccountCounterpartUSD { get; init; }

    [Range(1, int.MaxValue)]
    [Description("FK a la cuenta de gasto fallback usada cuando el SKU no tiene ProductAccount. Requerida para poder confirmar facturas.")]
    public int? IdDefaultExpenseAccount { get; init; }

    [Required]
    [Description("Indica si el tipo está activo")]
    public required bool IsActive { get; init; }
}
