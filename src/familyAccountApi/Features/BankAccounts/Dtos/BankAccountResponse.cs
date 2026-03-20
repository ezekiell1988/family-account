namespace FamilyAccountApi.Features.BankAccounts.Dtos;

public sealed record BankAccountResponse(
    int    IdBankAccount,
    int    IdBank,
    string CodeBank,
    string NameBank,
    int    IdAccount,
    string CodeAccount,
    string NameAccount,
    int    IdCurrency,
    string CodeCurrency,
    string NameCurrency,
    string CodeBankAccount,
    string AccountNumber,
    string AccountHolder,
    bool   IsActive);