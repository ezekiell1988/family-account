using System.ComponentModel;

namespace FamilyAccountApi.Features.Reports.Dtos;

/// <summary>
/// Filtro de período para los reportes financieros.
/// Usar UNA de las siguientes combinaciones:
///   - idFiscalPeriod : usa las fechas del período fiscal registrado.
///   - dateFrom + dateTo : rango explícito de fechas.
///   - year + month : un mes específico.
///   - year : año completo.
/// </summary>
public sealed record ReportFilterRequest
{
    [Description("ID del período fiscal. Si se proporciona, ignora las demás fechas.")]
    public int? IdFiscalPeriod { get; init; }

    [Description("Fecha de inicio del rango (inclusivo). Formato: yyyy-MM-dd.")]
    public DateOnly? DateFrom { get; init; }

    [Description("Fecha de fin del rango (inclusivo). Formato: yyyy-MM-dd.")]
    public DateOnly? DateTo { get; init; }

    [Description("Año del período. Úselo con 'month' para un mes, o solo para el año completo.")]
    public int? Year { get; init; }

    [Description("Mes del período (1-12). Úselo junto con 'year'.")]
    public int? Month { get; init; }
}
