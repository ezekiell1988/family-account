using System.ComponentModel.DataAnnotations;

namespace FamilyAccountApi.Features.SalesInvoices.Dtos;

public sealed record UpdateSalesInvoiceRequest
{
    [Required, Range(1, int.MaxValue)]
    public required int IdFiscalPeriod { get; init; }

    [Required, Range(1, int.MaxValue)]
    public required int IdCurrency { get; init; }

    [Required, Range(1, int.MaxValue)]
    public required int IdSalesInvoiceType { get; init; }

    [Range(1, int.MaxValue)]
    public int? IdContact { get; init; }

    [Range(1, int.MaxValue)]
    public int? IdBankAccount { get; init; }

    [Required]
    public required DateOnly DateInvoice { get; init; }

    [Range(0, double.MaxValue)]
    public required decimal SubTotalAmount { get; init; }

    [Range(0, double.MaxValue)]
    public required decimal TaxAmount { get; init; }

    [Range(0, double.MaxValue)]
    public required decimal TotalAmount { get; init; }

    [StringLength(500)]
    public string? DescriptionInvoice { get; init; }

    [Range(0.000001, double.MaxValue)]
    public required decimal ExchangeRateValue { get; init; }

    [Required, MinLength(1)]
    public required IReadOnlyList<SalesInvoiceLineRequest> Lines { get; init; }
}
