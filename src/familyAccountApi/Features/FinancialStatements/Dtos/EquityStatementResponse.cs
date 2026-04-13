namespace FamilyAccountApi.Features.FinancialStatements.Dtos;

/// <summary>
/// Estado de Cambios en el Patrimonio.
///
/// Muestra cómo varió cada cuenta de Capital durante el período.
/// NetIncome = resultado del período (aún en cuentas de Ingreso/Gasto; se
/// transferirá a Capital al cerrar el período contable).
///
/// Identidad:
///   TotalClosingEquity ≈ TotalOpeningEquity + TotalContributions
///                      - TotalWithdrawals + NetIncome
/// </summary>
public sealed record EquityStatementResponse(
    DateOnly DateFrom,
    DateOnly DateTo,
    // Resultado del período (Ingreso − Gasto)
    decimal  NetIncome,
    // Detalle por cuenta de Capital
    IReadOnlyList<EquityMovementResponse> Movements,
    // Totales
    decimal  TotalOpeningEquity,
    decimal  TotalContributions,
    decimal  TotalWithdrawals,
    decimal  TotalClosingEquity,
    // Patrimonio total incluyendo resultado del período aún no transferido
    decimal  TotalEquityIncludingNetIncome);
