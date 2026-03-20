namespace FamilyAccountApi.Features.BankStatementTemplates.Dtos;

public sealed record BankStatementTemplateResponse(
    int IdBankStatementTemplate,
    string CodeTemplate,
    string NameTemplate,
    string BankName,
    string ColumnMappings,
    string? DateFormat,
    string? TimeFormat,
    bool IsActive,
    string? Notes);
