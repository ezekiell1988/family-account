namespace FamilyAccountApi.Features.FinancialStatements.Dtos;

/// <summary>
/// Estado de Flujo de Efectivo — método indirecto simplificado.
///
/// Muestra los movimientos de apertura/cierre de todas las cuentas del balance
/// (Activo, Pasivo, Capital) más el resultado del período, para que el usuario
/// pueda identificar el cambio en sus cuentas de efectivo/banco.
///
/// Interpretación de cambios (Change):
///   Activo:  signo + = aumentó (se usó efectivo) | signo − = disminuyó (se liberó efectivo)
///   Pasivo:  signo + = aumentó (se recibió efectivo) | signo − = disminuyó (se pagó deuda)
///   Capital: signo + = aumentó (aporte) | signo − = disminuyó (retiro)
/// </summary>
public sealed record CashFlowStatementResponse(
    DateOnly DateFrom,
    DateOnly DateTo,
    // Resultado del período
    decimal  NetIncome,
    // Movimientos por tipo de cuenta
    IReadOnlyList<CashFlowLineResponse> AssetMovements,
    IReadOnlyList<CashFlowLineResponse> LiabilityMovements,
    IReadOnlyList<CashFlowLineResponse> EquityMovements,
    // Totales de cambio (suma de Change por sección)
    decimal  TotalAssetChange,
    decimal  TotalLiabilityChange,
    decimal  TotalEquityChange);
