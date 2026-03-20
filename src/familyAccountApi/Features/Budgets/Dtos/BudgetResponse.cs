namespace FamilyAccountApi.Features.Budgets.Dtos;

public sealed record BudgetResponse(
    int IdBudget,
    int IdAccount,
    string CodeAccount,
    string NameAccount,
    int IdFiscalPeriod,
    string NameFiscalPeriod,
    decimal AmountBudget,
    string? NotesBudget,
    bool IsActive);