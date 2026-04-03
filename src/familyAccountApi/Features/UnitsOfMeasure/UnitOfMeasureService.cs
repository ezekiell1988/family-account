using FamilyAccountApi.Domain.Entities;
using FamilyAccountApi.Features.UnitsOfMeasure.Dtos;
using FamilyAccountApi.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace FamilyAccountApi.Features.UnitsOfMeasure;

public sealed class UnitOfMeasureService(AppDbContext db) : IUnitOfMeasureService
{
    public async Task<IReadOnlyList<UnitOfMeasureResponse>> GetAllAsync(CancellationToken ct = default)
        => await db.UnitOfMeasure
            .AsNoTracking()
            .OrderBy(u => u.TypeUnit).ThenBy(u => u.CodeUnit)
            .Select(u => new UnitOfMeasureResponse(u.IdUnit, u.CodeUnit, u.NameUnit, u.TypeUnit))
            .ToListAsync(ct);

    public async Task<UnitOfMeasureResponse?> GetByIdAsync(int idUnit, CancellationToken ct = default)
        => await db.UnitOfMeasure
            .AsNoTracking()
            .Where(u => u.IdUnit == idUnit)
            .Select(u => new UnitOfMeasureResponse(u.IdUnit, u.CodeUnit, u.NameUnit, u.TypeUnit))
            .FirstOrDefaultAsync(ct);

    public async Task<UnitOfMeasureResponse> CreateAsync(CreateUnitOfMeasureRequest request, CancellationToken ct = default)
    {
        var entity = new UnitOfMeasure
        {
            CodeUnit = request.CodeUnit,
            NameUnit = request.NameUnit,
            TypeUnit = request.TypeUnit
        };

        db.UnitOfMeasure.Add(entity);
        await db.SaveChangesAsync(ct);

        return new UnitOfMeasureResponse(entity.IdUnit, entity.CodeUnit, entity.NameUnit, entity.TypeUnit);
    }

    public async Task<UnitOfMeasureResponse?> UpdateAsync(int idUnit, UpdateUnitOfMeasureRequest request, CancellationToken ct = default)
    {
        var entity = await db.UnitOfMeasure.FindAsync([idUnit], ct);
        if (entity is null) return null;

        entity.CodeUnit = request.CodeUnit;
        entity.NameUnit = request.NameUnit;
        entity.TypeUnit = request.TypeUnit;

        await db.SaveChangesAsync(ct);

        return new UnitOfMeasureResponse(entity.IdUnit, entity.CodeUnit, entity.NameUnit, entity.TypeUnit);
    }

    public async Task<bool> DeleteAsync(int idUnit, CancellationToken ct = default)
    {
        var deleted = await db.UnitOfMeasure
            .Where(u => u.IdUnit == idUnit)
            .ExecuteDeleteAsync(ct);

        return deleted > 0;
    }
}
