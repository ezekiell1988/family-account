using FamilyAccountApi.Domain.Entities;
using FamilyAccountApi.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace FamilyAccountApi.BackgroundJobs;

/// <summary>
/// Job de Hangfire que crea automáticamente los 12 períodos fiscales del año en curso.
/// Se programa para ejecutarse el 1° de enero a las 3:00 AM.
/// </summary>
public sealed class FiscalPeriodJobs(AppDbContext db)
{
    private static readonly string[] MonthNames =
    [
        "Enero", "Febrero", "Marzo", "Abril", "Mayo", "Junio",
        "Julio", "Agosto", "Septiembre", "Octubre", "Noviembre", "Diciembre"
    ];

    /// <summary>
    /// Crea los 12 períodos mensuales del año en curso si aún no existen.
    /// </summary>
    public async Task CreateCurrentYearPeriodsAsync()
    {
        var year = DateTime.UtcNow.Year;

        var existingMonths = await db.FiscalPeriod
            .Where(fp => fp.YearPeriod == year)
            .Select(fp => fp.MonthPeriod)
            .ToListAsync();

        var toCreate = new List<FiscalPeriod>();

        for (var month = 1; month <= 12; month++)
        {
            if (existingMonths.Contains(month)) continue;

            toCreate.Add(new FiscalPeriod
            {
                YearPeriod   = year,
                MonthPeriod  = month,
                NamePeriod   = $"{MonthNames[month - 1]} {year}",
                StatusPeriod = "Abierto",
                StartDate    = new DateOnly(year, month, 1),
                EndDate      = new DateOnly(year, month, DateTime.DaysInMonth(year, month))
            });
        }

        if (toCreate.Count > 0)
        {
            db.FiscalPeriod.AddRange(toCreate);
            await db.SaveChangesAsync();
        }
    }
}
