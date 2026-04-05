namespace FamilyAccountApi.Features.SalesInvoiceTypes.Dtos;

public sealed record SalesInvoiceTypeResponse(
    int     IdSalesInvoiceType,
    string  CodeSalesInvoiceType,
    string  NameSalesInvoiceType,
    bool    CounterpartFromBankMovement,
    int?    IdAccountCounterpartCRC,
    string? CodeAccountCounterpartCRC,
    string? NameAccountCounterpartCRC,
    int?    IdAccountCounterpartUSD,
    string? CodeAccountCounterpartUSD,
    string? NameAccountCounterpartUSD,
    int?    IdBankMovementType,
    int?    IdAccountSalesRevenue,
    string? CodeAccountSalesRevenue,
    string? NameAccountSalesRevenue,
    int?    IdAccountCOGS,
    string? CodeAccountCOGS,
    string? NameAccountCOGS,
    int?    IdAccountInventory,
    string? CodeAccountInventory,
    string? NameAccountInventory,
    bool    IsActive);
