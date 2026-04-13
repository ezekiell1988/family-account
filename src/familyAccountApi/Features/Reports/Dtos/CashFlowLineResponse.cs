namespace FamilyAccountApi.Features.Reports.Dtos;

/// <summary>
/// Movimiento de una cuenta durante el período (apertura → movimientos → cierre).
/// Balance de Activo  = TotalDebit  - TotalCredit  (saldo deudor).
/// Balance de Pasivo/Capital = TotalCredit - TotalDebit (saldo acreedor).
/// </summary>
public sealed record CashFlowLineResponse(
    int     IdAccount,
    string  CodeAccount,
    string  NameAccount,
    int     LevelAccount,
    decimal OpeningBalance,   // saldo acumulado antes del período
    decimal PeriodDebits,     // débitos durante el período
    decimal PeriodCredits,    // créditos durante el período
    decimal ClosingBalance,   // saldo acumulado al cierre del período
    decimal Change);          // ClosingBalance - OpeningBalance (signo = dirección normal del tipo)
