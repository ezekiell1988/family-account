namespace FamilyAccountApi.Features.BankStatementTransactions.Dtos;

public sealed record BankStatementTransactionResponse(
    int IdBankStatementTransaction,
    int IdBankStatementImport,
    DateOnly AccountingDate,
    DateOnly TransactionDate,
    TimeOnly? TransactionTime,
    string? DocumentNumber,
    string Description,
    decimal? DebitAmount,
    decimal? CreditAmount,
    decimal? Balance,
    bool IsReconciled,
    int? IdBankMovementType,
    string? BankMovementTypeName,
    string? MovementSign,
    int? IdAccountCounterpart,
    string? AccountCounterpartName,
    int? IdAccountingEntry);
