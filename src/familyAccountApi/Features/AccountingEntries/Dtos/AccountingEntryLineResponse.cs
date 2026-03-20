namespace FamilyAccountApi.Features.AccountingEntries.Dtos;

public sealed record AccountingEntryLineResponse(
    int     IdAccountingEntryLine,
    int     IdAccount,
    string  CodeAccount,
    string  NameAccount,
    decimal DebitAmount,
    decimal CreditAmount,
    string? DescriptionLine);
