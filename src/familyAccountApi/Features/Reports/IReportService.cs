using FamilyAccountApi.Features.Reports.Dtos;

namespace FamilyAccountApi.Features.Reports;

public interface IReportService
{
    /// <summary>
    /// Estado de Resultado para el período indicado.
    /// Solo incluye asientos publicados. Cuentas tipo Ingreso y Gasto.
    /// </summary>
    Task<IncomeStatementResponse> GetIncomeStatementAsync(
        ReportFilterRequest filter,
        CancellationToken ct = default);

    /// <summary>
    /// Estado de Situación Financiera acumulado hasta el fin del período indicado.
    /// Solo incluye asientos publicados. Cuentas tipo Activo, Pasivo y Capital.
    /// </summary>
    Task<BalanceSheetResponse> GetBalanceSheetAsync(
        ReportFilterRequest filter,
        CancellationToken ct = default);

    /// <summary>
    /// Estado de Flujo de Efectivo (método indirecto simplificado).
    /// Muestra movimientos de apertura/cierre de Activo, Pasivo y Capital más el
    /// resultado del período. Identifica las cuentas de caja/banco en la sección
    /// AssetMovements para calcular el cambio real de efectivo.
    /// </summary>
    Task<CashFlowStatementResponse> GetCashFlowStatementAsync(
        ReportFilterRequest filter,
        CancellationToken ct = default);

    /// <summary>
    /// Estado de Cambios en el Patrimonio.
    /// Muestra aportes, retiros y variación de cada cuenta de Capital durante el
    /// período, más el resultado del ejercicio aún no transferido.
    /// </summary>
    Task<EquityStatementResponse> GetEquityStatementAsync(
        ReportFilterRequest filter,
        CancellationToken ct = default);
}
