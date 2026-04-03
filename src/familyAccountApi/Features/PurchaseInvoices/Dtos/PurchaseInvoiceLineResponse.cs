namespace FamilyAccountApi.Features.PurchaseInvoices.Dtos;

public sealed record PurchaseInvoiceLineResponse(
    int      IdPurchaseInvoiceLine,
    int      IdPurchaseInvoice,
    int?     IdProduct,
    string?  NameProduct,
    int?     IdUnit,
    string?  CodeUnit,
    string   DescriptionLine,
    decimal  Quantity,
    decimal  QuantityBase,
    decimal  UnitPrice,
    decimal  TaxPercent,
    decimal  TotalLineAmount,
    string?  LotNumber,
    DateOnly? ExpirationDate);
