namespace FamilyAccountApi.Features.FinancialStatements.Dtos;

/// <summary>
/// Estado de Situación Financiera acumulado hasta 'DateTo'.
/// Incluye TODOS los asientos publicados desde el origen hasta DateTo (visión acumulativa).
/// Identidad contable: totalAssets ≈ totalLiabilitiesAndCapital.
/// </summary>
public sealed record BalanceSheetResponse(
    DateOnly                                   DateTo,
    IReadOnlyList<BalanceSheetLineResponse>    Assets,
    IReadOnlyList<BalanceSheetLineResponse>    Liabilities,
    IReadOnlyList<BalanceSheetLineResponse>    Capital,
    decimal                                    TotalAssets,
    decimal                                    TotalLiabilities,
    decimal                                    TotalCapital,
    decimal                                    TotalLiabilitiesAndCapital);
