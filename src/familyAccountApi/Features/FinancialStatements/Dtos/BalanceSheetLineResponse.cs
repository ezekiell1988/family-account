namespace FamilyAccountApi.Features.FinancialStatements.Dtos;

/// <summary>
/// Línea de cuenta del Estado de Situación Financiera.
/// Balance de Activo  = debitTotal  - creditTotal.
/// Balance de Pasivo  = creditTotal - debitTotal.
/// Balance de Capital = creditTotal - debitTotal.
/// </summary>
public sealed record BalanceSheetLineResponse(
    int     IdAccount,
    string  CodeAccount,
    string  NameAccount,
    int     LevelAccount,
    decimal DebitTotal,
    decimal CreditTotal,
    decimal Balance);
