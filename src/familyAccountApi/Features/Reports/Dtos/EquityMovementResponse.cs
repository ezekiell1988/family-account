namespace FamilyAccountApi.Features.Reports.Dtos;

/// <summary>
/// Movimiento de una cuenta de Capital durante el período.
/// OpeningBalance + CreditsInPeriod - DebitsInPeriod = ClosingBalance.
/// </summary>
public sealed record EquityMovementResponse(
    int     IdAccount,
    string  CodeAccount,
    string  NameAccount,
    int     LevelAccount,
    decimal OpeningBalance,    // saldo acreedor acumulado antes del período
    decimal CreditsInPeriod,   // aportes / aumentos durante el período
    decimal DebitsInPeriod,    // retiros / disminuciones durante el período
    decimal ClosingBalance);   // saldo acreedor acumulado al cierre del período
