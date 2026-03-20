using FamilyAccountApi.Domain.Entities;
using FamilyAccountApi.Features.FiscalPeriods.Dtos;
using FamilyAccountApi.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace FamilyAccountApi.Features.FiscalPeriods;

public sealed class FiscalPeriodService(AppDbContext db) : IFiscalPeriodService
{
    private static FiscalPeriodResponse ToResponse(FiscalPeriod fp) =>
        new(fp.IdFiscalPeriod, fp.YearPeriod, fp.MonthPeriod, fp.NamePeriod, fp.StatusPeriod, fp.StartDate, fp.EndDate);

    public async Task<IReadOnlyList<FiscalPeriodResponse>> GetAllAsync(CancellationToken ct = default)
    {
        return await db.FiscalPeriod
            .AsNoTracking()
            .OrderBy(fp => fp.YearPeriod)
            .ThenBy(fp => fp.MonthPeriod)
            .Select(fp => new FiscalPeriodResponse(fp.IdFiscalPeriod, fp.YearPeriod, fp.MonthPeriod, fp.NamePeriod, fp.StatusPeriod, fp.StartDate, fp.EndDate))
            .ToListAsync(ct);
    }

    public async Task<IReadOnlyList<FiscalPeriodResponse>> GetByYearAsync(int year, CancellationToken ct = default)
    {
        return await db.FiscalPeriod
            .AsNoTracking()
            .Where(fp => fp.YearPeriod == year)
            .OrderBy(fp => fp.MonthPeriod)
            .Select(fp => new FiscalPeriodResponse(fp.IdFiscalPeriod, fp.YearPeriod, fp.MonthPeriod, fp.NamePeriod, fp.StatusPeriod, fp.StartDate, fp.EndDate))
            .ToListAsync(ct);
    }

    public async Task<FiscalPeriodResponse?> GetByIdAsync(int idFiscalPeriod, CancellationToken ct = default)
    {
        return await db.FiscalPeriod
            .AsNoTracking()
            .Where(fp => fp.IdFiscalPeriod == idFiscalPeriod)
            .Select(fp => new FiscalPeriodResponse(fp.IdFiscalPeriod, fp.YearPeriod, fp.MonthPeriod, fp.NamePeriod, fp.StatusPeriod, fp.StartDate, fp.EndDate))
            .FirstOrDefaultAsync(ct);
    }

    public async Task<FiscalPeriodResponse> CreateAsync(CreateFiscalPeriodRequest request, CancellationToken ct = default)
    {
        var entity = new FiscalPeriod
        {
            YearPeriod   = request.YearPeriod,
            MonthPeriod  = request.MonthPeriod,
            NamePeriod   = request.NamePeriod,
            StatusPeriod = request.StatusPeriod,
            StartDate    = request.StartDate,
            EndDate      = request.EndDate
        };

        db.FiscalPeriod.Add(entity);
        await db.SaveChangesAsync(ct);

        return ToResponse(entity);
    }

    public async Task<FiscalPeriodResponse?> UpdateAsync(int idFiscalPeriod, UpdateFiscalPeriodRequest request, CancellationToken ct = default)
    {
        var entity = await db.FiscalPeriod.FindAsync([idFiscalPeriod], ct);
        if (entity is null) return null;

        entity.YearPeriod   = request.YearPeriod;
        entity.MonthPeriod  = request.MonthPeriod;
        entity.NamePeriod   = request.NamePeriod;
        entity.StatusPeriod = request.StatusPeriod;
        entity.StartDate    = request.StartDate;
        entity.EndDate      = request.EndDate;

        await db.SaveChangesAsync(ct);

        return ToResponse(entity);
    }

    public async Task<bool> DeleteAsync(int idFiscalPeriod, CancellationToken ct = default)
    {
        var deleted = await db.FiscalPeriod
            .Where(fp => fp.IdFiscalPeriod == idFiscalPeriod)
            .ExecuteDeleteAsync(ct);

        return deleted > 0;
    }
}
