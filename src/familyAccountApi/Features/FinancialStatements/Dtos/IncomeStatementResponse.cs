namespace FamilyAccountApi.Features.FinancialStatements.Dtos;

/// <summary>
/// Estado de Resultado para un período o rango de meses.
/// Incluye solo asientos publicados (statusEntry = 'Publicado').
/// NetIncome = totalRevenues - totalExpenses.
/// </summary>
public sealed record IncomeStatementResponse(
    DateOnly                                    DateFrom,
    DateOnly                                    DateTo,
    IReadOnlyList<IncomeStatementLineResponse>  Revenues,
    IReadOnlyList<IncomeStatementLineResponse>  Expenses,
    decimal                                     TotalRevenues,
    decimal                                     TotalExpenses,
    decimal                                     NetIncome);
