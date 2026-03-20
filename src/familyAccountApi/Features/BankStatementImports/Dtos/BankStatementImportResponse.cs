namespace FamilyAccountApi.Features.BankStatementImports.Dtos;

public sealed record BankStatementImportResponse(
    int IdBankStatementImport,
    int IdBankAccount,
    string BankAccountCode,
    string BankAccountName,
    int IdBankStatementTemplate,
    string TemplateCode,
    string TemplateName,
    string FileName,
    DateTime ImportDate,
    int ImportedBy,
    string ImportedByName,
    string Status,
    int TotalTransactions,
    int ProcessedTransactions,
    string? ErrorMessage);
