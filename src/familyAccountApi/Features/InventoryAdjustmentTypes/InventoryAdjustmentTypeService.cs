using FamilyAccountApi.Domain.Entities;
using FamilyAccountApi.Features.InventoryAdjustmentTypes.Dtos;
using FamilyAccountApi.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace FamilyAccountApi.Features.InventoryAdjustmentTypes;

public sealed class InventoryAdjustmentTypeService(AppDbContext db) : IInventoryAdjustmentTypeService
{
    private IQueryable<InventoryAdjustmentType> WithIncludes() =>
        db.InventoryAdjustmentType.AsNoTracking()
            .Include(iat => iat.IdAccountInventoryDefaultNavigation)
            .Include(iat => iat.IdAccountCounterpartEntryNavigation)
            .Include(iat => iat.IdAccountCounterpartExitNavigation);

    private static InventoryAdjustmentTypeResponse ToResponse(InventoryAdjustmentType iat) => new(
        iat.IdInventoryAdjustmentType,
        iat.CodeInventoryAdjustmentType,
        iat.NameInventoryAdjustmentType,
        iat.IdAccountInventoryDefault,
        iat.IdAccountInventoryDefaultNavigation?.CodeAccount,
        iat.IdAccountInventoryDefaultNavigation?.NameAccount,
        iat.IdAccountCounterpartEntry,
        iat.IdAccountCounterpartEntryNavigation?.CodeAccount,
        iat.IdAccountCounterpartEntryNavigation?.NameAccount,
        iat.IdAccountCounterpartExit,
        iat.IdAccountCounterpartExitNavigation?.CodeAccount,
        iat.IdAccountCounterpartExitNavigation?.NameAccount,
        iat.IsActive);

    public async Task<IReadOnlyList<InventoryAdjustmentTypeResponse>> GetAllAsync(CancellationToken ct = default) =>
        (await WithIncludes().OrderBy(iat => iat.IdInventoryAdjustmentType).ToListAsync(ct))
        .Select(ToResponse).ToList();

    public async Task<IReadOnlyList<InventoryAdjustmentTypeResponse>> GetActiveAsync(CancellationToken ct = default) =>
        (await WithIncludes().Where(iat => iat.IsActive).OrderBy(iat => iat.IdInventoryAdjustmentType).ToListAsync(ct))
        .Select(ToResponse).ToList();

    public async Task<InventoryAdjustmentTypeResponse?> GetByIdAsync(int idInventoryAdjustmentType, CancellationToken ct = default)
    {
        var entity = await WithIncludes()
            .FirstOrDefaultAsync(iat => iat.IdInventoryAdjustmentType == idInventoryAdjustmentType, ct);
        return entity is null ? null : ToResponse(entity);
    }

    public async Task<InventoryAdjustmentTypeResponse> CreateAsync(CreateInventoryAdjustmentTypeRequest request, CancellationToken ct = default)
    {
        var entity = new InventoryAdjustmentType
        {
            CodeInventoryAdjustmentType = request.CodeInventoryAdjustmentType.ToUpperInvariant().Trim(),
            NameInventoryAdjustmentType = request.NameInventoryAdjustmentType.Trim(),
            IdAccountInventoryDefault   = request.IdAccountInventoryDefault,
            IdAccountCounterpartEntry   = request.IdAccountCounterpartEntry,
            IdAccountCounterpartExit    = request.IdAccountCounterpartExit,
            IsActive                    = request.IsActive
        };

        db.InventoryAdjustmentType.Add(entity);
        await db.SaveChangesAsync(ct);

        return (await GetByIdAsync(entity.IdInventoryAdjustmentType, ct))!;
    }

    public async Task<InventoryAdjustmentTypeResponse?> UpdateAsync(int idInventoryAdjustmentType, UpdateInventoryAdjustmentTypeRequest request, CancellationToken ct = default)
    {
        var entity = await db.InventoryAdjustmentType.FindAsync([idInventoryAdjustmentType], ct);
        if (entity is null) return null;

        entity.NameInventoryAdjustmentType = request.NameInventoryAdjustmentType.Trim();
        entity.IdAccountInventoryDefault   = request.IdAccountInventoryDefault;
        entity.IdAccountCounterpartEntry   = request.IdAccountCounterpartEntry;
        entity.IdAccountCounterpartExit    = request.IdAccountCounterpartExit;
        entity.IsActive                    = request.IsActive;

        await db.SaveChangesAsync(ct);

        return await GetByIdAsync(idInventoryAdjustmentType, ct);
    }

    public async Task<bool> DeleteAsync(int idInventoryAdjustmentType, CancellationToken ct = default)
    {
        var deleted = await db.InventoryAdjustmentType
            .Where(iat => iat.IdInventoryAdjustmentType == idInventoryAdjustmentType)
            .ExecuteDeleteAsync(ct);

        return deleted > 0;
    }
}
