namespace FamilyAccountApi.Features.PurchaseInvoices.Dtos;

public sealed record PurchaseInvoiceLineResponse(
    int     IdPurchaseInvoiceLine,
    int     IdPurchaseInvoice,
    int?    IdProductSKU,
    string? CodeProductSKU,
    string  DescriptionLine,
    decimal Quantity,
    decimal UnitPrice,
    decimal TaxPercent,
    decimal TotalLineAmount);
