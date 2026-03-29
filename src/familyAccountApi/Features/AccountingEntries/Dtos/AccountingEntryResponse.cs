namespace FamilyAccountApi.Features.AccountingEntries.Dtos;

public sealed record AccountingEntryResponse(
    int      IdAccountingEntry,
    int      IdFiscalPeriod,
    string   NameFiscalPeriod,
    int      IdCurrency,
    string   CodeCurrency,
    string   NameCurrency,
    string   NumberEntry,
    DateOnly DateEntry,
    string   DescriptionEntry,
    string   StatusEntry,
    string?  ReferenceEntry,
    decimal  ExchangeRateValue,
    DateTime CreatedAt,
    IReadOnlyList<AccountingEntryLineResponse> Lines,
    bool     IsLinkedToBankMovement);
