namespace FamilyAccountApi.Features.PurchaseInvoiceTypes.Dtos;

public sealed record PurchaseInvoiceTypeResponse(
    int     IdPurchaseInvoiceType,
    string  CodePurchaseInvoiceType,
    string  NamePurchaseInvoiceType,
    bool    CounterpartFromBankMovement,
    int?    IdAccountCounterpartCRC,
    string? CodeAccountCounterpartCRC,
    string? NameAccountCounterpartCRC,
    int?    IdAccountCounterpartUSD,
    string? CodeAccountCounterpartUSD,
    string? NameAccountCounterpartUSD,
    int?    IdDefaultExpenseAccount,
    string? CodeDefaultExpenseAccount,
    string? NameDefaultExpenseAccount,
    bool    IsActive);
