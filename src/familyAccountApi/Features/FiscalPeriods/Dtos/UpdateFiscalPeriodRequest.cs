using System.ComponentModel;
using System.ComponentModel.DataAnnotations;

namespace FamilyAccountApi.Features.FiscalPeriods.Dtos;

public sealed record UpdateFiscalPeriodRequest
{
    [Required, Range(2000, 9999)]
    [Description("Año del período fiscal (p. ej. 2026)")]
    public required int YearPeriod { get; init; }

    [Required, Range(1, 12)]
    [Description("Mes del período: 1=Enero … 12=Diciembre")]
    public required int MonthPeriod { get; init; }

    [Required, StringLength(100, MinimumLength = 1)]
    [Description("Nombre descriptivo del período (p. ej. 'Enero 2026')")]
    public required string NamePeriod { get; init; }

    [Required]
    [AllowedValues("Abierto", "Cerrado", "Bloqueado",
        ErrorMessage = "El estado debe ser 'Abierto', 'Cerrado' o 'Bloqueado'.")]
    [Description("Estado del período: Abierto | Cerrado | Bloqueado")]
    public required string StatusPeriod { get; init; }

    [Required]
    [Description("Fecha de inicio del período (primer día del mes)")]
    public required DateOnly StartDate { get; init; }

    [Required]
    [Description("Fecha de fin del período (último día del mes)")]
    public required DateOnly EndDate { get; init; }
}
