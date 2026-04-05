using System.ComponentModel.DataAnnotations;

namespace FamilyAccountApi.Features.SalesInvoiceTypes.Dtos;

public sealed record UpdateSalesInvoiceTypeRequest
{
    [Required, StringLength(150, MinimumLength = 1)]
    public required string NameSalesInvoiceType { get; init; }

    [Required]
    public required bool CounterpartFromBankMovement { get; init; }

    [Range(1, int.MaxValue)]
    public int? IdAccountCounterpartCRC { get; init; }

    [Range(1, int.MaxValue)]
    public int? IdAccountCounterpartUSD { get; init; }

    [Range(1, int.MaxValue)]
    public int? IdBankMovementType { get; init; }

    [Range(1, int.MaxValue)]
    public int? IdAccountSalesRevenue { get; init; }

    [Range(1, int.MaxValue)]
    public int? IdAccountCOGS { get; init; }

    [Range(1, int.MaxValue)]
    public int? IdAccountInventory { get; init; }

    [Required]
    public required bool IsActive { get; init; }
}
