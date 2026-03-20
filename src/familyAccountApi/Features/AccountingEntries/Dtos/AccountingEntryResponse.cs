namespace FamilyAccountApi.Features.AccountingEntries.Dtos;

public sealed record AccountingEntryResponse(
    int      IdAccountingEntry,
    int      IdFiscalPeriod,
    string   NameFiscalPeriod,
    string   NumberEntry,
    DateOnly DateEntry,
    string   DescriptionEntry,
    string   StatusEntry,
    string?  ReferenceEntry,
    DateTime CreatedAt,
    IReadOnlyList<AccountingEntryLineResponse> Lines);
