namespace FamilyAccountApi.Features.SalesInvoices.Dtos;

public sealed record PartialReturnResponse(
    int      IdSalesInvoice,
    string   NumberInvoice,
    DateOnly DateReturn,
    string?  DescriptionReturn,
    decimal  TotalReturnAmount,
    int?     IdAccountingEntry,
    int?     IdAccountingEntryRefund);
