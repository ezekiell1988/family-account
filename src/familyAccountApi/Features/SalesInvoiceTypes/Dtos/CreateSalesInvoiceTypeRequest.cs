using System.ComponentModel;
using System.ComponentModel.DataAnnotations;

namespace FamilyAccountApi.Features.SalesInvoiceTypes.Dtos;

public sealed record CreateSalesInvoiceTypeRequest
{
    [Required, StringLength(20, MinimumLength = 1)]
    [Description("Código único del tipo (ej. 'CONTADO_CRC', 'CREDITO_CRC', 'CREDITO_USD')")]
    public required string CodeSalesInvoiceType { get; init; }

    [Required, StringLength(150, MinimumLength = 1)]
    [Description("Nombre descriptivo del tipo de factura de venta")]
    public required string NameSalesInvoiceType { get; init; }

    [Required]
    [Description("true = la cuenta DR se toma del BankAccount vinculado; false = cuenta Caja fija")]
    public required bool CounterpartFromBankMovement { get; init; }

    [Range(1, int.MaxValue)]
    [Description("FK a la cuenta Caja CRC (solo para tipo CONTADO_CRC)")]
    public int? IdAccountCounterpartCRC { get; init; }

    [Range(1, int.MaxValue)]
    [Description("FK a la cuenta Caja USD (solo para tipo CONTADO_USD)")]
    public int? IdAccountCounterpartUSD { get; init; }

    [Range(1, int.MaxValue)]
    [Description("FK al tipo de movimiento bancario para auto-crear el BankMovement (solo si CounterpartFromBankMovement = true)")]
    public int? IdBankMovementType { get; init; }

    [Range(1, int.MaxValue)]
    [Description("Cuenta CR de ingresos fallback cuando el producto no tiene ProductAccount configurado")]
    public int? IdAccountSalesRevenue { get; init; }

    [Range(1, int.MaxValue)]
    [Description("Cuenta DR de Costo de Ventas (COGS)")]
    public int? IdAccountCOGS { get; init; }

    [Range(1, int.MaxValue)]
    [Description("Cuenta CR de Inventario al reconocer el costo de ventas")]
    public int? IdAccountInventory { get; init; }

    [Required]
    [Description("Indica si el tipo está activo y disponible para registrar nuevas ventas")]
    public required bool IsActive { get; init; }
}
