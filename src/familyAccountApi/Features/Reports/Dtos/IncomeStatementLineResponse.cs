namespace FamilyAccountApi.Features.Reports.Dtos;

/// <summary>
/// Línea de cuenta del Estado de Resultado.
/// Balance de Ingreso = creditTotal - debitTotal.
/// Balance de Gasto   = debitTotal  - creditTotal.
/// </summary>
public sealed record IncomeStatementLineResponse(
    int     IdAccount,
    string  CodeAccount,
    string  NameAccount,
    int     LevelAccount,
    decimal DebitTotal,
    decimal CreditTotal,
    decimal Balance);
