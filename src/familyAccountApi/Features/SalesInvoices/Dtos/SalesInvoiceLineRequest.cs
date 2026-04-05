using System.ComponentModel.DataAnnotations;

namespace FamilyAccountApi.Features.SalesInvoices.Dtos;

public sealed record SalesInvoiceLineRequest
{
    /// <summary>
    /// true = línea de flete/servicio/gasto (IdInventoryLot puede ser null).
    /// false (default) = línea de producto con stock (IdInventoryLot requerido al confirmar).
    /// </summary>
    public bool IsNonProductLine { get; init; }

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
