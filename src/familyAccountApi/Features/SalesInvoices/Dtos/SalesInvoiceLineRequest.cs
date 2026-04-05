using System.ComponentModel.DataAnnotations;

namespace FamilyAccountApi.Features.SalesInvoices.Dtos;

public sealed record SalesInvoiceLineRequest
{
    [Range(1, int.MaxValue)]
    public int? IdProduct { get; init; }

    [Range(1, int.MaxValue)]
    public int? IdUnit { get; init; }

    [Range(1, int.MaxValue)]
    public int? IdInventoryLot { get; init; }

    [Required, StringLength(300, MinimumLength = 1)]
    public required string DescriptionLine { get; init; }

    [Range(0.0001, double.MaxValue)]
    public required decimal Quantity { get; init; }

    [Range(0, double.MaxValue)]
    public required decimal UnitPrice { get; init; }

    [Range(0, 100)]
    public required decimal TaxPercent { get; init; }

    [Range(0, double.MaxValue)]
    public required decimal TotalLineAmount { get; init; }
}
