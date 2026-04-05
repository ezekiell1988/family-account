namespace FamilyAccountApi.Features.SalesInvoices.Dtos;

public sealed record SalesInvoiceLineResponse(
    int      IdSalesInvoiceLine,
    int      IdSalesInvoice,
    bool     IsNonProductLine,
    int?     IdProduct,
    string?  NameProduct,
    int?     IdUnit,
    string?  CodeUnit,
    int?     IdInventoryLot,
    string?  LotNumber,
    string   DescriptionLine,
    decimal  Quantity,
    decimal  QuantityBase,
    decimal  UnitPrice,
    decimal? UnitCost,
    decimal  TaxPercent,
    decimal  TotalLineAmount);
