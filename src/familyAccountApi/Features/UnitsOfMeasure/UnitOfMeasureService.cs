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
            .OrderBy(u => u.UnitType.NameUnitType).ThenBy(u => u.CodeUnit)
            .Select(u => new UnitOfMeasureResponse(u.IdUnit, u.CodeUnit, u.NameUnit, u.IdUnitType, u.UnitType.NameUnitType))
            .ToListAsync(ct);

    public async Task<UnitOfMeasureResponse?> GetByIdAsync(int idUnit, CancellationToken ct = default)
        => await db.UnitOfMeasure
            .AsNoTracking()
            .Where(u => u.IdUnit == idUnit)
            .Select(u => new UnitOfMeasureResponse(u.IdUnit, u.CodeUnit, u.NameUnit, u.IdUnitType, u.UnitType.NameUnitType))
            .FirstOrDefaultAsync(ct);

    public async Task<UnitOfMeasureResponse> CreateAsync(CreateUnitOfMeasureRequest request, CancellationToken ct = default)
    {
        var entity = new UnitOfMeasure
        {
            CodeUnit   = request.CodeUnit,
            NameUnit   = request.NameUnit,
            IdUnitType = request.IdUnitType
        };

        db.UnitOfMeasure.Add(entity);
        await db.SaveChangesAsync(ct);

        await db.Entry(entity).Reference(u => u.UnitType).LoadAsync(ct);
        return new UnitOfMeasureResponse(entity.IdUnit, entity.CodeUnit, entity.NameUnit, entity.IdUnitType, entity.UnitType.NameUnitType);
    }

    public async Task<UnitOfMeasureResponse?> UpdateAsync(int idUnit, UpdateUnitOfMeasureRequest request, CancellationToken ct = default)
    {
        var entity = await db.UnitOfMeasure.FindAsync([idUnit], ct);
        if (entity is null) return null;

        entity.CodeUnit   = request.CodeUnit;
        entity.NameUnit   = request.NameUnit;
        entity.IdUnitType = request.IdUnitType;

        await db.SaveChangesAsync(ct);

        await db.Entry(entity).Reference(u => u.UnitType).LoadAsync(ct);
        return new UnitOfMeasureResponse(entity.IdUnit, entity.CodeUnit, entity.NameUnit, entity.IdUnitType, entity.UnitType.NameUnitType);
    }

    public async Task<bool> DeleteAsync(int idUnit, CancellationToken ct = default)
    {
        var deleted = await db.UnitOfMeasure
            .Where(u => u.IdUnit == idUnit)
            .ExecuteDeleteAsync(ct);

        return deleted > 0;
    }
}
