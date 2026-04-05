using FamilyAccountApi.Domain.Entities;
using FamilyAccountApi.Features.CostCenters.Dtos;
using FamilyAccountApi.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace FamilyAccountApi.Features.CostCenters;

public sealed class CostCenterService(AppDbContext db) : ICostCenterService
{
    public async Task<IReadOnlyList<CostCenterResponse>> GetAllAsync(CancellationToken ct = default)
    {
        return await db.CostCenter
            .AsNoTracking()
            .OrderBy(cc => cc.CodeCostCenter)
            .Select(cc => new CostCenterResponse(
                cc.IdCostCenter,
                cc.CodeCostCenter,
                cc.NameCostCenter,
                cc.IsActive))
            .ToListAsync(ct);
    }

    public async Task<CostCenterResponse?> GetByIdAsync(int idCostCenter, CancellationToken ct = default)
    {
        return await db.CostCenter
            .AsNoTracking()
            .Where(cc => cc.IdCostCenter == idCostCenter)
            .Select(cc => new CostCenterResponse(
                cc.IdCostCenter,
                cc.CodeCostCenter,
                cc.NameCostCenter,
                cc.IsActive))
            .FirstOrDefaultAsync(ct);
    }

    public async Task<CostCenterResponse> CreateAsync(CreateCostCenterRequest request, CancellationToken ct = default)
    {
        var entity = new CostCenter
        {
            CodeCostCenter = request.CodeCostCenter,
            NameCostCenter = request.NameCostCenter,
            IsActive       = request.IsActive
        };

        db.CostCenter.Add(entity);
        await db.SaveChangesAsync(ct);

        return new CostCenterResponse(
            entity.IdCostCenter,
            entity.CodeCostCenter,
            entity.NameCostCenter,
            entity.IsActive);
    }

    public async Task<CostCenterResponse?> UpdateAsync(int idCostCenter, UpdateCostCenterRequest request, CancellationToken ct = default)
    {
        var entity = await db.CostCenter.FindAsync([idCostCenter], ct);
        if (entity is null) return null;

        entity.CodeCostCenter = request.CodeCostCenter;
        entity.NameCostCenter = request.NameCostCenter;
        entity.IsActive       = request.IsActive;

        await db.SaveChangesAsync(ct);

        return new CostCenterResponse(
            entity.IdCostCenter,
            entity.CodeCostCenter,
            entity.NameCostCenter,
            entity.IsActive);
    }

    public async Task<bool> DeleteAsync(int idCostCenter, CancellationToken ct = default)
    {
        var entity = await db.CostCenter.FindAsync([idCostCenter], ct);
        if (entity is null) return false;

        entity.IsActive = false;
        await db.SaveChangesAsync(ct);
        return true;
    }
}
