using FamilyAccountApi.Domain.Entities;
using FamilyAccountApi.Features.Warehouses.Dtos;
using FamilyAccountApi.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace FamilyAccountApi.Features.Warehouses;

public sealed class WarehouseService(AppDbContext db) : IWarehouseService
{
    private static WarehouseResponse ToResponse(Warehouse w) =>
        new(w.IdWarehouse, w.NameWarehouse, w.IsDefault, w.IsActive);

    public async Task<IReadOnlyList<WarehouseResponse>> GetAllAsync(CancellationToken ct = default) =>
        await db.Warehouse
            .AsNoTracking()
            .OrderBy(w => w.NameWarehouse)
            .Select(w => new WarehouseResponse(w.IdWarehouse, w.NameWarehouse, w.IsDefault, w.IsActive))
            .ToListAsync(ct);

    public async Task<WarehouseResponse?> GetByIdAsync(int idWarehouse, CancellationToken ct = default)
    {
        var entity = await db.Warehouse.AsNoTracking()
            .FirstOrDefaultAsync(w => w.IdWarehouse == idWarehouse, ct);
        return entity is null ? null : ToResponse(entity);
    }

    public async Task<WarehouseResponse> CreateAsync(CreateWarehouseRequest request, CancellationToken ct = default)
    {
        if (request.IsDefault)
            await ClearDefaultAsync(ct);

        var entity = new Warehouse
        {
            NameWarehouse = request.NameWarehouse.Trim(),
            IsDefault     = request.IsDefault,
            IsActive      = request.IsActive
        };

        db.Warehouse.Add(entity);
        await db.SaveChangesAsync(ct);

        return ToResponse(entity);
    }

    public async Task<WarehouseResponse?> UpdateAsync(int idWarehouse, UpdateWarehouseRequest request, CancellationToken ct = default)
    {
        var entity = await db.Warehouse.FindAsync([idWarehouse], ct);
        if (entity is null) return null;

        if (request.IsDefault && !entity.IsDefault)
            await ClearDefaultAsync(ct);

        entity.NameWarehouse = request.NameWarehouse.Trim();
        entity.IsDefault     = request.IsDefault;
        entity.IsActive      = request.IsActive;

        await db.SaveChangesAsync(ct);
        return ToResponse(entity);
    }

    public async Task<bool> DeleteAsync(int idWarehouse, CancellationToken ct = default)
    {
        var hasLots = await db.InventoryLot
            .AnyAsync(il => il.IdWarehouse == idWarehouse, ct);

        if (hasLots)
            throw new InvalidOperationException(
                "No se puede eliminar el almacén porque tiene lotes de inventario asociados. " +
                "Transfiera o anule los lotes antes de eliminar el almacén.");

        var deleted = await db.Warehouse
            .Where(w => w.IdWarehouse == idWarehouse)
            .ExecuteDeleteAsync(CancellationToken.None);

        return deleted > 0;
    }

    private async Task ClearDefaultAsync(CancellationToken ct) =>
        await db.Warehouse
            .Where(w => w.IsDefault)
            .ExecuteUpdateAsync(s => s.SetProperty(w => w.IsDefault, false), ct);
}
